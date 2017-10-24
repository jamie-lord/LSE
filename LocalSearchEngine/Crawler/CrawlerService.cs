using System;
using System.Threading;
using LocalSearchEngine.Database;

namespace LocalSearchEngine.Crawler
{
    public class CrawlerService
    {
        private static readonly PageManager _pageManager = new PageManager();

        public void Start()
        {
            while (true)
            {
                var newPage = _pageManager.NextToCrawl();
                if (newPage != null)
                {
                    var crawler = new Crawler();
                    var result = crawler.CrawlAsync(newPage.Uri);
                    result.Wait();

                    _pageManager.RemoveNewPage(newPage);
                    _pageManager.AddCrawledPage(result.Result.Item1);
                    _pageManager.AddNewPages(result.Result.Item2);

                    Console.WriteLine($"Found {result.Result.Item2.Count} URIs on {result.Result.Item1.Uri}");
                    result.Result.Item1.PrintDetails();
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
