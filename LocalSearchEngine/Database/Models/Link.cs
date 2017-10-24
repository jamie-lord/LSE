﻿using System;
using SQLite;

namespace LocalSearchEngine.Database.Models
{
    public class Link
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public string Uri { get; set; }
        public DateTime Added { get; set; }
        [Indexed]
        public int PageFoundOn { get; set; }
    }
}
