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
        [Indexed, NotNull]
        public string Uri { get; set; }
        [Indexed, NotNull]
        public DateTime? LastCheck { get; set; }
        public string TextDirection { get; set; }
        public string Language { get; set; }
        public int Length { get; set; }
        public DateTime? PublicationDate { get; set; }
        public TimeSpan? TimeToRead { get; set; }

        [Ignore]
        public PageContent Content { get; set; }

        [Ignore]
        public List<Dictionary<string, string>> MetaTags
        {
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
            TextDirection = pageMetadata.TextDirection;
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

            if (!string.IsNullOrEmpty(pageMetadata.TextContent) ||
                !string.IsNullOrEmpty(pageMetadata.Title) ||
                !string.IsNullOrEmpty(pageMetadata.Author) ||
                !string.IsNullOrEmpty(pageMetadata.ByLine) ||
                !string.IsNullOrWhiteSpace(pageMetadata.Excerpt))
            {
                Content = new PageContent
                {
                    Title = pageMetadata.Title,
                    Content = pageMetadata.TextContent,
                    Author = pageMetadata.Author,
                    ByLine = pageMetadata.ByLine,
                    Excerpt = pageMetadata.Excerpt
                };
            }
        }

        public void PrintDetails()
        {
            PrintProperty(Id, nameof(Id));
            PrintProperty(Uri, nameof(Uri));
            PrintProperty(Content?.Title, nameof(Content.Title));
            PrintProperty(LastCheck, nameof(LastCheck));
            PrintProperty(Content?.Author, nameof(Content.Author));
            PrintProperty(Content?.ByLine, nameof(Content.ByLine));
            PrintProperty(TextDirection, nameof(TextDirection));
            PrintProperty(Content?.Excerpt, nameof(Content.Excerpt));
            PrintProperty(Language, nameof(Language));
            PrintProperty(Length, nameof(Length));
            PrintProperty(PublicationDate, nameof(PublicationDate));
            PrintProperty(TimeToRead, nameof(TimeToRead));
            if (MetaTags != null) PrintProperty(MetaTags.Count, nameof(MetaTags));
            Console.WriteLine("************");
        }

        private void PrintProperty(object p, string name)
        {
            if (p != null) Console.WriteLine("{0,-20}{1,-20}", name, p);
        }
    }
}
