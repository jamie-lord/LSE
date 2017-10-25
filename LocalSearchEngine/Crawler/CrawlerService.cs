using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LocalSearchEngine.Database;
using LocalSearchEngine.Database.Models;

namespace LocalSearchEngine.Crawler
{
    public class CrawlerService
    {
        private static readonly PageManager _pageManager = new PageManager();

        public void Start()
        {
            var queueWatcherTask = Task.Run(() => ResultQueueProcessor());

            while (true)
            {
                var link = _pageManager.NextToCrawl();
                if (link != null)
                {
                    var crawler = new Crawler();
                    var result = crawler.CrawlAsync(link.Uri);
                    result.Wait();

                    _resultQueue.Enqueue(new CrawlResult
                    {
                        Page = result.Result.Item1,
                        Links = result.Result.Item2,
                        LinkToRemove = link
                    });
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private class CrawlResult
        {
            public Page Page { get; set; }
            public List<Link> Links { get; set; }
            public Link LinkToRemove { get; set; }
        }

        private ConcurrentQueue<CrawlResult> _resultQueue = new ConcurrentQueue<CrawlResult>();

        private void ResultQueueProcessor()
        {
            while (true)
            {
                if (_resultQueue.IsEmpty)
                {
                    Thread.Sleep(500);
                }
                else
                {
                    _resultQueue.TryDequeue(out var r);
                    if (r != null)
                    {
                        _pageManager.RemoveLink(r.LinkToRemove);
                        _pageManager.UpdatePage(r.Page);

                        r.Links.ForEach(link => link.PageFoundOn = r.Page.Id);

                        _pageManager.UpdateLinks(r.Links);

                        Console.WriteLine($"Found {r.Links.Count} URIs on {r.Page.Uri}");
                        r.Page.PrintDetails();
                    }
                }
            }
        }
    }
}
