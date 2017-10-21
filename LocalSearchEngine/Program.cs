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
            var pageManager = new PageManager();

            pageManager.AddNewPage(new Link { Uri = new Uri("https://www.theguardian.com/uk"), Added = DateTime.Now });
            pageManager.AddNewPage(new Link { Uri = new Uri("https://lord.technology"), Added = DateTime.Now });
            pageManager.AddNewPage(new Link { Uri = new Uri("https://arstechnica.co.uk/"), Added = DateTime.Now });

            pageManager.Dispose();
        }
    }
}
