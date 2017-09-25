using System;
using System.IO;
using System.Reflection;

namespace LocalSearchEngine
{
    public class Program
    {
        private static readonly string _workingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        private static readonly PageManager _pageManager = new PageManager();

        public static void Main(string[] args)
        {
            //var hoot = new Hoot.Hoot($"{_workingDir}/Indexes", "test", false);

            //hoot.FreeMemory();

            //hoot.Index(0, "this is some great text!");

            //hoot.OptimizeIndex();

            SeedNewPages();

            NewPageCrawl();
        }

        private static void SeedNewPages()
        {
            _pageManager.AddNewPage(new NewPage { Uri = new Uri("https://www.theguardian.com/uk"), Added = DateTime.Now });
            _pageManager.AddNewPage(new NewPage { Uri = new Uri("https://lord.technology"), Added = DateTime.Now });
            _pageManager.AddNewPage(new NewPage { Uri = new Uri("https://arstechnica.co.uk/"), Added = DateTime.Now });
        }

        private static void NewPageCrawl()
        {
            var newPage = _pageManager.NextToCrawl();
            if (newPage == null)
            {
                return;
            }

            var crawler = new Crawler();
            crawler.Crawl(newPage.Uri);
            _pageManager.RemoveNewPage(newPage);
        }
    }
}
