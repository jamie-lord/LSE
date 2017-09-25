using System;
namespace LocalSearchEngine
{
    public class CrawledPage
    {
        public int Id { get; set; }
        public Uri Uri { get; set; }
        public DateTime? LastCheck { get; set; }
        public string Content { get; set; }
    }
}
