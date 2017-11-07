using System;
using System.Text.RegularExpressions;

namespace LocalSearchEngine.Crawler
{
    public class PageMetadata
    {
        private string _author;

        public string Author
        {
            get { return _author; }
            set { _author = value; }
        }

        private string _byLine;

        public string ByLine
        {
            get { return _byLine; }
            set { _byLine = value; }
        }

        private string _textDirection;

        public string TextDirection
        {
            get { return _textDirection; }
            set { _textDirection = value; }
        }

        private string _excerpt;

        public string Excerpt
        {
            get { return _excerpt; }
            set { _excerpt = value; }
        }

        private string _language;

        public string Language
        {
            get { return _language; }
            set { _language = value; }
        }

        private int _length;

        public int Length
        {
            get { return _length; }
            set { _length = value; }
        }

        private DateTime? _publicationDate;

        public DateTime? PublicationDate
        {
            get { return _publicationDate; }
            set { _publicationDate = value; }
        }

        private TimeSpan _timeToRead;

        public TimeSpan TimeToRead
        {
            get { return _timeToRead; }
            set { _timeToRead = value; }
        }

        private string _title;

        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        private string _uri;

        public string Uri
        {
            get { return _uri; }
            set { _uri = value; }
        }

        private static readonly Regex _multipleSpaces = new Regex(@"\s+", RegexOptions.Compiled);

        private string _textContent;

        public string TextContent
        {
            get { return _textContent; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _textContent = _multipleSpaces.Replace(value, " ");
                    _textContent = _textContent.Trim();
                }
                else
                {
                    _textContent = value;
                }
            }
        }

        public PageMetadata()
        {
        }

        public PageMetadata(string author, string byLine, string textDirection, string excerpt, string language, int length, DateTime? publicationDate, TimeSpan timeToRead, string title, string uri, string textContent)
        {
            _author = author;
            _byLine = byLine;
            _textDirection = textDirection;
            _excerpt = excerpt;
            _language = language;
            _length = length;
            _publicationDate = publicationDate;
            _timeToRead = timeToRead;
            _title = title;
            _uri = uri;
            TextContent = textContent;
        }
    }
}
