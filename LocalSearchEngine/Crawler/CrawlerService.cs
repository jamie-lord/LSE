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
                    var result = crawler.Crawl(newPage.Uri);
                    _pageManager.RemoveNewPage(newPage);

                    _pageManager.AddCrawledPages(result.Item1);
                    _pageManager.AddNewPages(result.Item2);
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
