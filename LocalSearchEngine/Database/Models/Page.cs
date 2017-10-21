using System;
namespace LocalSearchEngine.Database.Models
{
    public class Page
    {
        public int Id { get; set; }
        public Uri Uri { get; set; }
        public DateTime? LastCheck { get; set; }
        public string Content { get; set; }
    }
}
