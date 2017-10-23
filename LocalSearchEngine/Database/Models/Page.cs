using System;
using LocalSearchEngine.Crawler;

namespace LocalSearchEngine.Database.Models
{
    public class Page
    {
        public int Id { get; set; }
        public Uri Uri { get; set; }
        public DateTime? LastCheck { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public string ByLine { get; set; }
        public string TextDirection { get; set; }
        public string Excerpt { get; set; }
        public string Language { get; set; }
        public int Length { get; set; }
        public DateTime? PublicationDate { get; set; }
        public TimeSpan TimeToRead { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }

        public void InsertMetadata(PageMetadata pageMetadata)
        {
            Author = pageMetadata.Author;
            ByLine = pageMetadata.ByLine;
            TextDirection = pageMetadata.TextDirection;
            Excerpt = pageMetadata.Excerpt;
            Language = pageMetadata.Language;
            Length = pageMetadata.Length;
            PublicationDate = pageMetadata.PublicationDate;
            TimeToRead = pageMetadata.TimeToRead;
            Title = pageMetadata.Title;
            Text = pageMetadata.TextContent;
        }
    }
}
