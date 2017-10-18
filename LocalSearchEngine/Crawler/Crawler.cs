using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Easy.Common;
using Easy.Common.Extensions;
using Easy.Common.Interfaces;
using HtmlAgilityPack;

namespace LocalSearchEngine
{
    public class Crawler
    {
        private ConcurrentBag<NewPage> _newPages = new ConcurrentBag<NewPage>();

        public Crawler()
        {
        }

        private readonly Dictionary<string, string> _defaultHeaders = new Dictionary<string, string>
        {
            {"Accept", "application/json"},
            {"UserAgent", "foo-bar"}
        };

        public async Task<(CrawledPage, ConcurrentBag<NewPage>)> CrawlAsync(Uri uri)
        {
            _newPages = new ConcurrentBag<NewPage>();

            var crawledPage = new CrawledPage();

            using (IRestClient client = new RestClient(_defaultHeaders, timeout: 15.Seconds()))
            {
                using (var response = await client.SendAsync(new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = uri }))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return (crawledPage, _newPages);
                    }

                    if (response.Headers.Date.HasValue)
                    {
                        crawledPage.LastCheck = response.Headers.Date.Value.DateTime;
                    }
                    else
                    {
                        crawledPage.LastCheck = DateTime.Now;
                    }

                    crawledPage.Uri = response.RequestMessage.RequestUri;

                    using (var content = response.Content)
                    {
                        var result = await content.ReadAsStringAsync();
                        crawledPage.Content = result;
                        var document = new HtmlDocument();
                        document.LoadHtml(result);

                        foreach (HtmlNode link in document.DocumentNode.SelectNodes("//a[@href]"))
                        {
                            HtmlAttribute att = link.Attributes["href"];

                            _newPages.Add(new NewPage {
                                Uri = new Uri(att.Value),
                                Added = crawledPage.LastCheck.Value,
                                FoundOn = crawledPage.Uri
                            });
                        }
                    }
                }
            }

            return (crawledPage, _newPages);
        }
    }
}
