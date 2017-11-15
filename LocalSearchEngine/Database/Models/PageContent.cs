using SQLite;

namespace LocalSearchEngine.Database.Models
{
    public class PageContent
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public string ByLine { get; set; }
        public string Excerpt { get; set; }
    }
}
