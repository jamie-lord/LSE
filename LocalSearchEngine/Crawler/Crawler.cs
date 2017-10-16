using System;
using System.Collections.Concurrent;
using System.Net;
using Abot.Crawler;
using Abot.Poco;

namespace LocalSearchEngine
{
    public class Crawler
    {
        private readonly CrawlConfiguration _crawlConfig = new CrawlConfiguration
        {
            CrawlTimeoutSeconds = 0,
            HttpRequestTimeoutInSeconds = 15,
            MinRetryDelayInMilliseconds = 200,
            MaxConcurrentThreads = 25,
            MaxPagesToCrawl = 0,
            IsExternalPageCrawlingEnabled = true,
            IsRespectRobotsDotTextEnabled = true,
            UserAgentString = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.91 Safari/537.36 Vivaldi/1.93.955.36"
        };

        private PoliteWebCrawler _crawler;

        private ConcurrentQueue<CrawledPage> _crawledPages = new ConcurrentQueue<CrawledPage>();

        private ConcurrentQueue<NewPage> _newPages = new ConcurrentQueue<NewPage>();

        public Crawler()
        {
            _crawler = new PoliteWebCrawler(_crawlConfig);

            _crawler.PageCrawlStartingAsync += ProcessPageCrawlStarting;
            _crawler.PageCrawlCompletedAsync += ProcessPageCrawlCompleted;
            //_crawler.PageCrawlDisallowedAsync += PageCrawlDisallowed;
            //_crawler.PageLinksCrawlDisallowedAsync += PageLinksCrawlDisallowed;
        }

        public (ConcurrentQueue<CrawledPage>, ConcurrentQueue<NewPage>) Crawl(Uri uri)
        {
            _crawledPages = new ConcurrentQueue<CrawledPage>();
            _newPages = new ConcurrentQueue<NewPage>();

            var task = _crawler.CrawlAsync(uri);
            task.Wait();

            return (_crawledPages, _newPages);
        }

        private void ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine("About to crawl link {0} which was found on page {1}", pageToCrawl.Uri.AbsoluteUri, pageToCrawl.ParentUri.AbsoluteUri);
        }

        private void ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            Abot.Poco.CrawledPage crawledPage = e.CrawledPage;

            if (crawledPage.HttpRequestException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
                return;
            }
            else
            {
                Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);

                if (string.IsNullOrEmpty(crawledPage.Content.Text))
                {
                    Console.WriteLine("Page had no content {0}", crawledPage.Uri.AbsoluteUri);
                    return;
                }

                var htmlAgilityPackDocument = crawledPage.HtmlDocument; //Html Agility Pack parser

                var page = new CrawledPage
                {
                    LastCheck = crawledPage.RequestStarted,
                    Uri = crawledPage.Uri,
                    Content = crawledPage.Content.Text
                };

                _crawledPages.Enqueue(page);

                foreach (var link in crawledPage.ParsedLinks)
                {
                    _newPages.Enqueue(new NewPage { Uri = link, Added = crawledPage.RequestStarted, FoundOn = crawledPage.Uri });
                }
            }
        }

        private void PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            Abot.Poco.CrawledPage crawledPage = e.CrawledPage;
            Console.WriteLine("Did not crawl the links on page {0} due to {1}", crawledPage.Uri.AbsoluteUri, e.DisallowedReason);

            // TODO: Store uris of pages where link crawls are disallowed
        }

        private void PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine("Did not crawl page {0} due to {1}", pageToCrawl.Uri.AbsoluteUri, e.DisallowedReason);

            // TODO: Store pages where crawling is disallowed
        }
    }
}
