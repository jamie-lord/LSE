using System;
using SQLite;

namespace LocalSearchEngine.Database.Models
{
    public class Link
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed, NotNull]
        public string Uri { get; set; }
        [Indexed, NotNull]
        public DateTime Added { get; set; }
        public int PageFoundOn { get; set; }
    }
}
