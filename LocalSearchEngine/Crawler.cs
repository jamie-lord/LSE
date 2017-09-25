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
            MinRetryDelayInMilliseconds = 1000,
            MaxConcurrentThreads = 10,
            MaxPagesToCrawl = 0,
            IsExternalPageCrawlingEnabled = true,
            IsRespectRobotsDotTextEnabled = true,
            UserAgentString = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.91 Safari/537.36 Vivaldi/1.93.955.36"
        };

        private PoliteWebCrawler _crawler;

        public ConcurrentQueue<CrawledPage> CrawledPages = new ConcurrentQueue<CrawledPage>();

        public ConcurrentQueue<NewPage> NewPages = new ConcurrentQueue<NewPage>();

        public Crawler()
        {
            _crawler = new PoliteWebCrawler(_crawlConfig);

            _crawler.PageCrawlStartingAsync += ProcessPageCrawlStarting;
            _crawler.PageCrawlCompletedAsync += ProcessPageCrawlCompleted;
            _crawler.PageCrawlDisallowedAsync += PageCrawlDisallowed;
            _crawler.PageLinksCrawlDisallowedAsync += PageLinksCrawlDisallowed;
        }

        private bool _crawling = false;

        public void Crawl(Uri uri)
        {
            if (!_crawling)
            {
                _crawling = true;
                var task = _crawler.CrawlAsync(uri);
                task.Wait();
                _crawling = false;
            }
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
                var angleSharpHtmlDocument = crawledPage.AngleSharpHtmlDocument; //AngleSharp parser

                var page = new CrawledPage
                {
                    LastCheck = crawledPage.RequestStarted,
                    Uri = crawledPage.Uri,
                    Content = crawledPage.Content.Text
                };

                CrawledPages.Enqueue(page);

                foreach (var link in crawledPage.ParsedLinks)
                {
                    NewPages.Enqueue(new NewPage { Uri = link, Added = crawledPage.RequestStarted });
                }
            }
        }

        private void PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            Abot.Poco.CrawledPage crawledPage = e.CrawledPage;
            Console.WriteLine("Did not crawl the links on page {0} due to {1}", crawledPage.Uri.AbsoluteUri, e.DisallowedReason);
        }

        private void PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine("Did not crawl page {0} due to {1}", pageToCrawl.Uri.AbsoluteUri, e.DisallowedReason);
        }
    }
}
