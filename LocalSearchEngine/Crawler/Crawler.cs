using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace LocalSearchEngine
{
    public class Crawler
    {
        private ConcurrentBag<NewPage> _newPages = new ConcurrentBag<NewPage>();

        public Crawler()
        {
        }

        public async Task<(CrawledPage, ConcurrentBag<NewPage>)> CrawlAsync(Uri uri)
        {
            _newPages = new ConcurrentBag<NewPage>();

            var crawledPage = new CrawledPage();

            HttpClient client = new HttpClient();

            using (var response = await client.GetAsync(uri))
            {
                using (var content = response.Content)
                {
                    var result = await content.ReadAsStringAsync();
                    var document = new HtmlDocument();
                    document.LoadHtml(result);

                    //crawledPage.LastCheck = 
                }
            }

            return (crawledPage, _newPages);
        }

        //private void ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        //{
        //    PageToCrawl pageToCrawl = e.PageToCrawl;
        //    Console.WriteLine("About to crawl link {0} which was found on page {1}", pageToCrawl.Uri.AbsoluteUri, pageToCrawl.ParentUri.AbsoluteUri);
        //}

        //private void ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        //{
        //    if (e.CrawledPage.HttpRequestException != null || e.CrawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
        //    {
        //        Console.WriteLine("Crawl of page failed {0}", e.CrawledPage.Uri.AbsoluteUri);
        //        return;
        //    }
        //    else
        //    {
        //        Console.WriteLine("Crawl of page succeeded {0}", e.CrawledPage.Uri.AbsoluteUri);

        //        if (string.IsNullOrEmpty(e.CrawledPage.Content.Text))
        //        {
        //            Console.WriteLine("Page had no content {0}", e.CrawledPage.Uri.AbsoluteUri);
        //            return;
        //        }

        //        //var htmlAgilityPackDocument = crawledPage.HtmlDocument; //Html Agility Pack parser

        //        var page = new CrawledPage
        //        {
        //            LastCheck = e.CrawledPage.RequestStarted,
        //            Uri = e.CrawledPage.Uri,
        //            Content = e.CrawledPage.Content.Text
        //        };

        //        _crawledPages.Add(page);

        //        foreach (var link in e.CrawledPage.ParsedLinks)
        //        {
        //            _newPages.Add(new NewPage { Uri = link, Added = e.CrawledPage.RequestStarted, FoundOn = e.CrawledPage.Uri });
        //        }
        //    }
        //}
    }
}
