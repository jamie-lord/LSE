using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace Hoot.MGIndex
{
    // high frequency storage file with overwrite old values
    internal class StorageFileHF
    {
        FileStream _dataWrite;
        WAHBitArray _freeList;
        Action<WAHBitArray> _saveFreeList;
        Func<WAHBitArray> _readFreeList;

        private string _fileName = "";
        private object _readLock = new object();
        //ILog _log = LogManager.GetLogger(typeof(StorageFileHF));

        // **** change this if storage format changed ****
        internal static int _currentVersion = 1;
        private int _lastBlockNumber = 0;
        private ushort _blockSize = 4096;
        private string _path = "";
        private string _s = Path.DirectorySeparatorChar.ToString();

        public static byte[] _fileHeader = { (byte)'M', (byte)'G', (byte)'H', (byte)'F',
                                              0,   // 4 -- storage file version number,
                                              0,2, // 5,6 -- block size ushort low, hi
                                              1    // 7 -- key type 0 = guid, 1 = string
                                           };

        public StorageFileHF(string filename, ushort blocksize) : this(filename, blocksize, null, null)
        {
        }

        // used for bitmapindexhf
        public StorageFileHF(string filename, ushort blocksize, Func<WAHBitArray> readfreelist, Action<WAHBitArray> savefreelist)
        {
            _saveFreeList = savefreelist;
            _readFreeList = readfreelist;
            _path = Path.GetDirectoryName(filename);
            if (_path.EndsWith(_s) == false) _path += _s;
            _fileName = Path.GetFileNameWithoutExtension(filename);

            Initialize(filename, blocksize);
        }

        public void Shutdown()
        {
            // write free list 
            if (_saveFreeList != null)
                _saveFreeList(_freeList);
            else
                WriteFreeListBMPFile(_path + _fileName + ".free");
            FlushClose(_dataWrite);
            _dataWrite = null;
        }

        public ushort GetBlockSize()
        {
            return _blockSize;
        }

        internal void FreeBlocks(List<int> list)
        {
            list.ForEach(x => _freeList.Set(x, true));
        }

        internal byte[] ReadBlock(int blocknumber)
        {
            SeekBlock(blocknumber);
            byte[] data = new byte[_blockSize];
            _dataWrite.Read(data, 0, _blockSize);

            return data;
        }

        internal byte[] ReadBlockBytes(int blocknumber, int bytes)
        {
            SeekBlock(blocknumber);
            byte[] data = new byte[bytes];
            _dataWrite.Read(data, 0, bytes);

            return data;
        }

        internal int GetFreeBlockNumber()
        {
            // get the first free block or append to the end
            if (_freeList.CountOnes() > 0)
            {
                int i = _freeList.GetFirst();
                _freeList.Set(i, false);
                return i;
            }
            else
                return Interlocked.Increment(ref _lastBlockNumber);//++;
        }

        internal void Initialize()
        {
            if (_readFreeList != null)
                _freeList = _readFreeList();
            else
            {
                _freeList = new WAHBitArray();
                if (File.Exists(_path + _fileName + ".free"))
                {
                    ReadFreeListBMPFile(_path + _fileName + ".free");
                    // delete file so if failure no big deal on restart
                    File.Delete(_path + _fileName + ".free");
                }
            }
        }

        internal void SeekBlock(int blocknumber)
        {
            long offset = (long)_fileHeader.Length + (long)blocknumber * _blockSize;
            _dataWrite.Seek(offset, SeekOrigin.Begin);// wiil seek past the end of file on fs.Write will zero the difference
        }

        internal void WriteBlockBytes(byte[] data, int start, int len)
        {
            _dataWrite.Write(data, start, len);
        }

        #region [ private / internal  ]

        private void WriteFreeListBMPFile(string filename)
        {
            if (_freeList != null)
            {
                WAHBitArray.TYPE t;
                uint[] ints = _freeList.GetCompressed(out t);
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write((byte)t);// write new format with the data type byte
                foreach (var i in ints)
                {
                    bw.Write(i);
                }
                File.WriteAllBytes(filename, ms.ToArray());
            }
        }

        private void ReadFreeListBMPFile(string filename)
        {
            byte[] b = File.ReadAllBytes(filename);
            WAHBitArray.TYPE t = WAHBitArray.TYPE.WAH;
            int j = 0;
            if (b.Length % 4 > 0) // new format with the data type byte
            {
                t = (WAHBitArray.TYPE)Enum.ToObject(typeof(WAHBitArray.TYPE), b[0]);
                j = 1;
            }
            List<uint> ints = new List<uint>();
            for (int i = 0; i < b.Length / 4; i++)
            {
                ints.Add((uint)Helper.ToInt32(b, (i * 4) + j));
            }
            _freeList = new WAHBitArray(t, ints.ToArray());
        }

        private void Initialize(string filename, ushort blocksize)
        {
            if (File.Exists(filename) == false)
                _dataWrite = new FileStream(filename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
            else
                _dataWrite = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            if (_dataWrite.Length == 0)
            {
                CreateFileHeader(blocksize);
                // new file
                _dataWrite.Write(_fileHeader, 0, _fileHeader.Length);
                _dataWrite.Flush();
            }
            else
            {
                ReadFileHeader();
                _lastBlockNumber = (int)((_dataWrite.Length - _fileHeader.Length) / _blockSize);
                _lastBlockNumber++;
            }
            //if (_readfreeList != null)
            //    _freeList = _readfreeList();
            //else
            //{
            //    _freeList = new WAHBitArray();
            //    if (File.Exists(_Path + _filename + ".free"))
            //    {
            //        ReadFreeListBMPFile(_Path + _filename + ".free");
            //        // delete file so if failure no big deal on restart
            //        File.Delete(_Path + _filename + ".free");
            //    }
            //}
        }

        private void ReadFileHeader()
        {
            // set _blockize
            _dataWrite.Seek(0L, SeekOrigin.Begin);
            byte[] hdr = new byte[_fileHeader.Length];
            _dataWrite.Read(hdr, 0, _fileHeader.Length);

            _blockSize = 0;
            _blockSize = (ushort)((int)hdr[5] + ((int)hdr[6]) << 8);
        }

        private void CreateFileHeader(int blocksize)
        {
            // add version number
            _fileHeader[4] = (byte)_currentVersion;
            // block size
            _fileHeader[5] = (byte)(blocksize & 0xff);
            _fileHeader[6] = (byte)(blocksize >> 8);
            _blockSize = (ushort)blocksize;
        }

        private void FlushClose(FileStream st)
        {
            if (st != null)
            {
                st.Flush(true);
                st.Close();
            }
        }
        #endregion

        internal int NumberofBlocks()
        {
            return (int)((_dataWrite.Length / (int)_blockSize) + 1);
        }

        internal void FreeBlock(int i)
        {
            _freeList.Set(i, true);
        }
    }
}
