using System;
using System.Threading;

namespace LocalSearchEngine
{
    public class CrawlerService
    {
        public CrawlerService()
        {
        }

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
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
