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
            //var hoot = new Hoot.Hoot($"{_workingDir}/Indexes", "test", false);

            //hoot.FreeMemory();

            //hoot.Index(0, "this is some great text!");

            //hoot.OptimizeIndex();

            SeedNewPages();

            var crawlerService = new CrawlerService();
            crawlerService.Start();
        }

        private static void SeedNewPages()
        {
            var pageManager = new PageManager(true);

            pageManager.UpdateLink(new Link { Uri = "https://www.theguardian.com/uk", Added = DateTime.Now });
            pageManager.UpdateLink(new Link { Uri = "https://lord.technology", Added = DateTime.Now });
            pageManager.UpdateLink(new Link { Uri = "https://arstechnica.co.uk/", Added = DateTime.Now });

            pageManager.Dispose();
        }
    }
}
