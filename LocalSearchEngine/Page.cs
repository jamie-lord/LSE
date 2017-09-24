using System;
namespace LocalSearchEngine
{
    public class Page
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public DateTime? LastCheck { get; set; }
        public string Content { get; set; }
        public string Title { get; set; }
    }
}
