using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Hoot.MGIndex
{
    internal class StorageData<T>
    {
        public StorageItem<T> meta;
        public byte[] data;
    }

    public class StorageItem<T>
    {
        public T Key;
        public string TypeName;
        public DateTime Date = FastDateTime.Now;
        public bool IsDeleted;
        public bool IsReplicated;
        public int DataLength;
        public byte IsCompressed; // 0 = no, 1 = MiniLZO
    }

    public interface IDocStorage<T>
    {
        int RecordCount();

        byte[] GetBytes(int rowid, out StorageItem<T> meta);
        object GetObject(int rowid, out StorageItem<T> meta);
        StorageItem<T> GetMeta(int rowid);

        bool GetObject(T key, out object doc);
    }

    public enum SF_FORMAT
    {
        BSON,
        JSON
    }

    internal struct SplitFile
    {
        public long Start;
        public long UpToLength;
        public FileStream File;
    }

    internal class StorageFile<T>
    {
        private FileStream _datawrite;
        private FileStream _recfilewrite;
        private FileStream _recfileread = null;
        private FileStream _dataread = null;

        private string _fileName = "";
        private string _recFileName = "";
        private long _lastRecordNum = 0;
        private long _lastWriteOffset = _fileHeader.Length;
        private object _readLock = new object();
        private bool _dirty = false;
        private IGetBytes<T> _t = null;
        private ILog _log = LogManager.GetLogger(typeof(StorageFile<T>));
        private SF_FORMAT _saveFormat = SF_FORMAT.BSON;

        // **** change this if storage format changed ****
        internal static int _currentVersion = 2;

        //private ushort _splitMegaBytes = 0; // 0 = off 
        //private bool _enableSplits = false;
        private List<SplitFile> _files = new List<SplitFile>();
        private List<long> _uptoIndexes = new List<long>();
        // no splits in view mode 
        private bool _viewMode = false;
        private SplitFile _lastSplitFile;

        public static byte[] _fileHeader = { (byte)'M', (byte)'G', (byte)'D', (byte)'B',
                                              0, // 4 -- storage file version number,
                                              0  // 5 -- not used
                                           };
        private static string _splitFileExtension = "00000";
        private const int _kilobyte = 1024;
        // record format :
        //    1 type (0 = raw no meta data, 1 = bson meta, 2 = json meta)  
        //    4 byte meta/data length, 
        //    n byte meta serialized data if exists 
        //    m byte data (if meta exists then m is in meta.dataLength)

        /// <summary>
        /// View data storage mode (no splits, bson save) 
        /// </summary>
        /// <param name="filename"></param>
        public StorageFile(string filename)
        {
            _viewMode = true;
            _saveFormat = SF_FORMAT.BSON;
            // add version number
            _fileHeader[5] = (byte)_currentVersion;
            Initialize(filename, false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        /// <param name="StorageOnlyMode">= true -> don't create mgrec files (used for backup and replication mode)</param>
        public StorageFile(string filename, SF_FORMAT format, bool StorageOnlyMode)
        {
            _saveFormat = format;
            if (StorageOnlyMode) _viewMode = true; // no file splits
            // add version number
            _fileHeader[5] = (byte)_currentVersion;
            Initialize(filename, StorageOnlyMode);
        }

        private StorageFile(string filename, bool StorageOnlyMode)
        {
            Initialize(filename, StorageOnlyMode);
        }

        private void Initialize(string filename, bool StorageOnlyMode)
        {
            _t = RDBDataType<T>.ByteHandler();
            _fileName = filename;

            // search for mgdat00000 extensions -> split files load
            if (File.Exists(filename + _splitFileExtension))
            {
                LoadSplitFiles(filename);
            }

            if (File.Exists(filename) == false)
                _datawrite = new FileStream(filename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
            else
                _datawrite = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            _dataread = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (_datawrite.Length == 0)
            {
                // new file
                _datawrite.Write(_fileHeader, 0, _fileHeader.Length);
                _datawrite.Flush();
                _lastWriteOffset = _fileHeader.Length;
            }
            else
            {
                long i = _datawrite.Seek(0L, SeekOrigin.End);
                if (_files.Count == 0)
                    _lastWriteOffset = i;
                else
                    _lastWriteOffset += i; // add to the splits
            }

            if (StorageOnlyMode == false)
            {
                // load rec pointers
                _recFileName = filename.Substring(0, filename.LastIndexOf('.')) + ".mgrec";
                if (File.Exists(_recFileName) == false)
                    _recfilewrite = new FileStream(_recFileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
                else
                    _recfilewrite = new FileStream(_recFileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);

                _recfileread = new FileStream(_recFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                _lastRecordNum = (int)(_recfilewrite.Length / 8);
                _recfilewrite.Seek(0L, SeekOrigin.End);
            }
        }

        private void LoadSplitFiles(string filename)
        {
            _log.Debug("Loading split files...");
            _lastWriteOffset = 0;
            for (int i = 0; ; i++)
            {
                string _filename = filename + i.ToString(_splitFileExtension);
                if (File.Exists(_filename) == false)
                    break;
                FileStream file = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                SplitFile sf = new SplitFile();
                sf.Start = _lastWriteOffset;
                _lastWriteOffset += file.Length;
                sf.File = file;
                sf.UpToLength = _lastWriteOffset;
                _files.Add(sf);
                _uptoIndexes.Add(sf.UpToLength);
            }
            _lastSplitFile = _files[_files.Count - 1];
            _log.Debug("Number of split files = " + _files.Count);
        }

        public static int GetStorageFileHeaderVersion(string filename)
        {
            string fn = filename + _splitFileExtension; // if split files -> load the header from the first file -> mgdat00000
            if (File.Exists(fn) == false)
                fn = filename; // else use the mgdat file 

            if (File.Exists(fn))
            {
                var fs = new FileStream(fn, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                fs.Seek(0L, SeekOrigin.Begin);
                byte[] b = new byte[_fileHeader.Length];
                fs.Read(b, 0, _fileHeader.Length);
                fs.Close();
                return b[5];
            }
            return _currentVersion;
        }

        public int Count()
        {
            return (int)_lastRecordNum;// (int)(_recfilewrite.Length >> 3);
        }

        public long WriteRawData(byte[] b)
        {
            return internalWriteData(null, b, true);
        }

        public long Delete(T key)
        {
            StorageItem<T> meta = new StorageItem<T>();
            meta.Key = key;
            meta.IsDeleted = true;

            return internalWriteData(meta, null, false);
        }

        public long DeleteReplicated(T key)
        {
            StorageItem<T> meta = new StorageItem<T>();
            meta.Key = key;
            meta.IsReplicated = true;
            meta.IsDeleted = true;

            return internalWriteData(meta, null, false);
        }

        public long WriteObject(T key, object obj)
        {
            StorageItem<T> meta = new StorageItem<T>();
            meta.Key = key;
            meta.TypeName = fastJSON.Reflection.Instance.GetTypeAssemblyName(obj.GetType());
            byte[] data;
            if (_saveFormat == SF_FORMAT.BSON)
                data = fastBinaryJSON.BJSON.ToBJSON(obj);
            else
                data = Helper.GetBytes(fastJSON.JSON.ToJSON(obj));
            if (data.Length > (int)Global.CompressDocumentOverKiloBytes * _kilobyte)
            {
                meta.IsCompressed = 1;
                data = MiniLZO.Compress(data); //MiniLZO
            }
            return internalWriteData(meta, data, false);
        }

        public long WriteReplicationObject(T key, object obj)
        {
            StorageItem<T> meta = new StorageItem<T>();
            meta.Key = key;
            meta.IsReplicated = true;
            meta.TypeName = fastJSON.Reflection.Instance.GetTypeAssemblyName(obj.GetType());
            byte[] data;
            if (_saveFormat == SF_FORMAT.BSON)
                data = fastBinaryJSON.BJSON.ToBJSON(obj);
            else
                data = Helper.GetBytes(fastJSON.JSON.ToJSON(obj));
            if (data.Length > (int)Global.CompressDocumentOverKiloBytes * _kilobyte)
            {
                meta.IsCompressed = 1;
                data = MiniLZO.Compress(data);
            }
            return internalWriteData(meta, data, false);
        }

        public long WriteData(T key, byte[] data)
        {
            StorageItem<T> meta = new StorageItem<T>();
            meta.Key = key;

            if (data.Length > (int)Global.CompressDocumentOverKiloBytes * _kilobyte)
            {
                meta.IsCompressed = 1;
                data = MiniLZO.Compress(data);
            }

            return internalWriteData(meta, data, false);
        }

        public byte[] ReadBytes(long recnum)
        {
            StorageItem<T> meta;
            return ReadBytes(recnum, out meta);
        }

        public object ReadObject(long recnum)
        {
            StorageItem<T> meta = null;
            return ReadObject(recnum, out meta);
        }

        public object ReadObject(long recnum, out StorageItem<T> meta)
        {
            byte[] b = ReadBytes(recnum, out meta);

            if (b == null)
                return null;
            if (b[0] < 32)
                return fastBinaryJSON.BJSON.ToObject(b);
            else
                return fastJSON.JSON.ToObject(Encoding.ASCII.GetString(b));
        }

        /// <summary>
        /// used for views only
        /// </summary>
        /// <param name="recnum"></param>
        /// <returns></returns>
        public byte[] ViewReadRawBytes(long recnum)
        {
            // views can't be split
            if (recnum >= _lastRecordNum)
                return null;

            lock (_readLock)
            {
                long offset = ComputeOffset(recnum);
                _dataread.Seek(offset, System.IO.SeekOrigin.Begin);
                byte[] hdr = new byte[5];
                // read header
                _dataread.Read(hdr, 0, 5); // meta length
                int len = Helper.ToInt32(hdr, 1);

                int type = hdr[0];
                if (type == 0)
                {
                    byte[] data = new byte[len];
                    _dataread.Read(data, 0, len);
                    return data;
                }
                return null;
            }
        }

        public void Shutdown()
        {
            if (_files.Count > 0)
                _files.ForEach(s => FlushClose(s.File));

            FlushClose(_dataread);
            FlushClose(_recfileread);
            FlushClose(_recfilewrite);
            FlushClose(_datawrite);

            _dataread = null;
            _recfileread = null;
            _recfilewrite = null;
            _datawrite = null;
        }

        public static StorageFile<Guid> ReadForward(string filename)
        {
            StorageFile<Guid> sf = new StorageFile<Guid>(filename, true);

            return sf;
        }

        public StorageItem<T> ReadMeta(long rowid)
        {
            if (rowid >= _lastRecordNum)
                return null;
            lock (_readLock)
            {
                int metalen = 0;
                long off = ComputeOffset(rowid);
                FileStream fs = GetReadFileStreamWithSeek(off);
                StorageItem<T> meta = ReadMetaData(fs, out metalen);
                return meta;
            }
        }

        #region [ private / internal  ]

        private long internalWriteData(StorageItem<T> meta, byte[] data, bool raw)
        {
            lock (_readLock)
            {
                _dirty = true;
                // seek end of file
                long offset = _lastWriteOffset;
                if (_viewMode == false && Global.SplitStorageFilesMegaBytes > 0)
                {
                    // current file size > _splitMegaBytes --> new file
                    if (offset > (long)Global.SplitStorageFilesMegaBytes * 1024 * 1024)
                        CreateNewStorageFile();
                }

                if (raw == false)
                {
                    if (data != null)
                        meta.DataLength = data.Length;
                    byte[] metabytes = fastBinaryJSON.BJSON.ToBJSON(meta, new fastBinaryJSON.BJSONParameters { UseExtensions = false });

                    // write header info
                    _datawrite.Write(new byte[] { 1 }, 0, 1); // FEATURE : add json here, write bson for now
                    _datawrite.Write(Helper.GetBytes(metabytes.Length, false), 0, 4);
                    _datawrite.Write(metabytes, 0, metabytes.Length);
                    // update pointer
                    _lastWriteOffset += metabytes.Length + 5;
                }
                else
                {
                    // write header info
                    _datawrite.Write(new byte[] { 0 }, 0, 1); // write raw
                    _datawrite.Write(Helper.GetBytes(data.Length, false), 0, 4);
                    // update pointer
                    _lastWriteOffset += 5;
                }

                if (data != null)
                {
                    // write data block
                    _datawrite.Write(data, 0, data.Length);
                    _lastWriteOffset += data.Length;
                }
                // return starting offset -> recno
                long recno = _lastRecordNum++;
                if (_recfilewrite != null)
                    _recfilewrite.Write(Helper.GetBytes(offset, false), 0, 8);
                if (Global.FlushStorageFileImmediately)
                {
                    _datawrite.Flush();
                    if (_recfilewrite != null)
                        _recfilewrite.Flush();
                }
                return recno;
            }
        }

        private void CreateNewStorageFile()
        {
            _log.Debug("Split limit reached = " + _datawrite.Length);
            int i = _files.Count;
            // close files
            FlushClose(_datawrite);
            FlushClose(_dataread);
            long start = 0;
            if (i > 0)
                start = _lastSplitFile.UpToLength; // last file offset
            // rename mgdat to mgdat0000n
            File.Move(_fileName, _fileName + i.ToString(_splitFileExtension));
            FileStream file = new FileStream(_fileName + i.ToString(_splitFileExtension), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            SplitFile sf = new SplitFile();
            sf.Start = start;
            sf.UpToLength = _lastWriteOffset;
            sf.File = file;
            _files.Add(sf);

            _uptoIndexes.Add(sf.UpToLength);

            _lastSplitFile = sf;
            // new mgdat file
            _datawrite = new FileStream(_fileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
            _dataread = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _log.Debug("New storage file created, count = " + _files.Count);
        }

        internal byte[] ReadBytes(long recnum, out StorageItem<T> meta)
        {
            meta = null;
            if (recnum >= _lastRecordNum)
                return null;
            lock (_readLock)
            {
                long off = ComputeOffset(recnum);
                FileStream fs = GetReadFileStreamWithSeek(off);
                byte[] data = internalReadBytes(fs, out meta);

                if (meta.IsCompressed > 0)
                    data = MiniLZO.Decompress(data);

                return data;
            }
        }

        private long ComputeOffset(long recnum)
        {
            if (_dirty)
            {
                _datawrite.Flush();
                _recfilewrite.Flush();
            }
            long off = recnum << 3;// *8L;
            byte[] b = new byte[8];

            _recfileread.Seek(off, SeekOrigin.Begin);
            _recfileread.Read(b, 0, 8);
            off = Helper.ToInt64(b, 0);
            if (off == 0)// kludge
                off = 6;
            return off;
        }

        private byte[] internalReadBytes(FileStream fs, out StorageItem<T> meta)
        {
            int metalen = 0;
            meta = ReadMetaData(fs, out metalen);
            if (meta != null)
            {
                if (meta.IsDeleted == false)
                {
                    byte[] data = new byte[meta.DataLength];
                    fs.Read(data, 0, meta.DataLength);
                    return data;
                }
            }
            else
            {
                byte[] data = new byte[metalen];
                fs.Read(data, 0, metalen);
                return data;
            }
            return null;
        }

        private StorageItem<T> ReadMetaData(FileStream fs, out int metasize)
        {
            byte[] hdr = new byte[5];
            // read header
            fs.Read(hdr, 0, 5); // meta length
            int len = Helper.ToInt32(hdr, 1);
            int type = hdr[0];
            if (type > 0)
            {
                metasize = len + 5;
                hdr = new byte[len];
                fs.Read(hdr, 0, len);
                StorageItem<T> meta;
                if (type == 1)
                    meta = fastBinaryJSON.BJSON.ToObject<StorageItem<T>>(hdr);
                else
                {
                    string str = Helper.GetString(hdr, 0, (short)hdr.Length);
                    meta = fastJSON.JSON.ToObject<StorageItem<T>>(str);
                }
                return meta;
            }
            else
            {
                metasize = len;
                return null;
            }
        }

        private void FlushClose(FileStream st)
        {
            if (st != null)
            {
                st.Flush(true);
                st.Close();
            }
        }

        internal T GetKey(long recnum, out bool deleted)
        {
            lock (_readLock)
            {
                deleted = false;
                long off = ComputeOffset(recnum);
                FileStream fs = GetReadFileStreamWithSeek(off);

                int metalen = 0;
                StorageItem<T> meta = ReadMetaData(fs, out metalen);
                deleted = meta.IsDeleted;
                return meta.Key;
            }
        }

        internal int CopyTo(StorageFile<T> storageFile, long startrecord)
        {
            FileStream fs;
            bool inthefiles = false;
            // copy data here
            lock (_readLock)
            {
                long off = ComputeOffset(startrecord);
                fs = GetReadFileStreamWithSeek(off);
                if (fs != _dataread)
                    inthefiles = true;
                Pump(fs, storageFile._datawrite);
            }

            // pump the remainder of the files also 
            if (inthefiles && _files.Count > 0)
            {
                long off = ComputeOffset(startrecord);
                int i = binarysearch(off);
                i++; // next file stream
                for (int j = i; j < _files.Count; j++)
                {
                    lock (_readLock)
                    {
                        fs = _files[j].File;
                        fs.Seek(0L, SeekOrigin.Begin);
                        Pump(fs, storageFile._datawrite);
                    }
                }

                // pump the current mgdat
                lock (_readLock)
                {
                    _dataread.Seek(0L, SeekOrigin.Begin);
                    Pump(_dataread, storageFile._datawrite);
                }
            }

            return (int)_lastRecordNum;
        }

        private static void Pump(Stream input, Stream output)
        {
            byte[] bytes = new byte[4096 * 2];
            int n;
            while ((n = input.Read(bytes, 0, bytes.Length)) != 0)
                output.Write(bytes, 0, n);
        }

        internal IEnumerable<StorageData<T>> ReadOnlyEnumerate()
        {
            // MGREC files may not exist

            //// the total number of records 
            //long count = _recfileread.Length >> 3;

            //for (long i = 0; i < count; i++)
            //{
            //    StorageItem<T> meta;
            //    byte[] data = ReadBytes(i, out meta);
            //    StorageData<T> sd = new StorageData<T>();
            //    sd.meta = meta;
            //    if (meta.dataLength > 0)
            //        sd.data = data;

            //    yield return sd;
            //}

            long offset = _fileHeader.Length;// start; // skip header
            long size = _dataread.Length;
            while (offset < size)
            {
                StorageData<T> sd = new StorageData<T>();
                lock (_readLock)
                {
                    _dataread.Seek(offset, SeekOrigin.Begin);
                    int metalen = 0;
                    StorageItem<T> meta = ReadMetaData(_dataread, out metalen);
                    offset += metalen;

                    sd.meta = meta;
                    if (meta.DataLength > 0)
                    {
                        byte[] data = new byte[meta.DataLength];
                        _dataread.Read(data, 0, meta.DataLength);
                        sd.data = data;
                    }
                    offset += meta.DataLength;
                }
                yield return sd;
            }
        }

        private FileStream GetReadFileStreamWithSeek(long offset)
        {
            long fileoffset = offset;
            // search split _files for offset and compute fileoffset in the file
            if (_files.Count > 0) // we have splits
            {
                if (offset < _lastSplitFile.UpToLength) // offset is in the list
                {
                    int i = binarysearch(offset);
                    var f = _files[i];
                    fileoffset -= f.Start; // offset in the file 
                    f.File.Seek(fileoffset, SeekOrigin.Begin);
                    return f.File;
                }
                else
                    fileoffset -= _lastSplitFile.UpToLength; // offset in the mgdat file
            }

            // seek to position in file 
            _dataread.Seek(fileoffset, SeekOrigin.Begin);
            return _dataread;
        }

        private int binarysearch(long offset)
        {
            //// binary search
            int low = 0;
            int high = _files.Count - 1;
            int midpoint = 0;
            int lastlower = 0;

            while (low <= high)
            {
                midpoint = low + (high - low) / 2;
                long k = _uptoIndexes[midpoint];
                // check to see if value is equal to item in array
                if (offset == k)
                    return midpoint + 1;
                else if (offset < k)
                {
                    high = midpoint - 1;
                    lastlower = midpoint;
                }
                else
                    low = midpoint + 1;
            }

            return lastlower;
        }
        #endregion
    }
}
