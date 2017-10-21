﻿using System;

namespace LocalSearchEngine.Database.Models
{
    public class NewPage
    {
        public int Id { get; set; }
        public Uri Uri { get; set; }
        public DateTime Added { get; set; }
        public Uri FoundOn { get; set; }
    }
}
