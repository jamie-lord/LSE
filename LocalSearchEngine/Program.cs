using System;
using LocalSearchEngine.Crawler;
using LocalSearchEngine.Database;
using LocalSearchEngine.Database.Models;

namespace LocalSearchEngine
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SeedNewPages();

            var crawlerService = new CrawlerService();
            crawlerService.Start();
        }

        private static void SeedNewPages()
        {
            var pageManager = new DatabaseManager(true);

            pageManager.UpdateLink(new Link { Uri = "https://www.theguardian.com/uk", Added = DateTime.Now });
            pageManager.UpdateLink(new Link { Uri = "https://lord.technology", Added = DateTime.Now });
            pageManager.UpdateLink(new Link { Uri = "https://arstechnica.co.uk/", Added = DateTime.Now });

            pageManager.Dispose();
        }
    }
}
