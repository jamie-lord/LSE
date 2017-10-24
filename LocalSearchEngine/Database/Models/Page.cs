using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using LocalSearchEngine.Crawler;
using SQLite;

namespace LocalSearchEngine.Database.Models
{
    public class Page
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public string Uri { get; set; }
        [Indexed]
        public DateTime? LastCheck { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public string ByLine { get; set; }
        public string TextDirection { get; set; }
        public string Excerpt { get; set; }
        public string Language { get; set; }
        public int Length { get; set; }
        public DateTime? PublicationDate { get; set; }
        public TimeSpan? TimeToRead { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        [Ignore]
        public List<Dictionary<string, string>> MetaTags {
            get
            {
                if (BinaryMetaTags == null)
                {
                    return null;
                }
                var stream = new MemoryStream();
                var binFormatter = new BinaryFormatter();
                stream.Write(BinaryMetaTags, 0, BinaryMetaTags.Length);
                stream.Position = 0;
                return binFormatter.Deserialize(stream) as List<Dictionary<string, string>>;
            }
            set
            {
                var binFormatter = new BinaryFormatter();
                var stream = new MemoryStream();
                binFormatter.Serialize(stream, value);
                BinaryMetaTags = stream.ToArray();
            }
        }
        public byte[] BinaryMetaTags { get; set; }
        public void InsertMetadata(PageMetadata pageMetadata)
        {
            Author = pageMetadata.Author;
            ByLine = pageMetadata.ByLine;
            TextDirection = pageMetadata.TextDirection;
            Excerpt = pageMetadata.Excerpt;
            Language = pageMetadata.Language;
            Length = pageMetadata.Length;
            PublicationDate = pageMetadata.PublicationDate;

            if (pageMetadata.TimeToRead == TimeSpan.Zero)
            {
                TimeToRead = null;
            }
            else
            {
                TimeToRead = pageMetadata.TimeToRead;
            }

            Title = pageMetadata.Title;
            Text = pageMetadata.TextContent;
        }

        public void PrintDetails()
        {
            PrintProperty(Id, nameof(Id));
            PrintProperty(Uri, nameof(Uri));
            PrintProperty(Title, nameof(Title));
            PrintProperty(LastCheck, nameof(LastCheck));
            PrintProperty(Author, nameof(Author));
            PrintProperty(ByLine, nameof(ByLine));
            PrintProperty(TextDirection, nameof(TextDirection));
            PrintProperty(Excerpt, nameof(Excerpt));
            PrintProperty(Language, nameof(Language));
            PrintProperty(Length, nameof(Length));
            PrintProperty(PublicationDate, nameof(PublicationDate));
            PrintProperty(TimeToRead, nameof(TimeToRead));
            if (MetaTags != null) PrintProperty(MetaTags.Count, nameof(MetaTags));
            Console.WriteLine("************");
        }

        private void PrintProperty(object p, string name)
        {
            if (p != null) Console.WriteLine("{0,-15}{1,-15}", name, p);
        }
    }
}
