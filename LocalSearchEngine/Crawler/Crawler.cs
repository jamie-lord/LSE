using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Easy.Common;
using Easy.Common.Extensions;
using Easy.Common.Interfaces;
using HtmlAgilityPack;

namespace LocalSearchEngine.Crawler
{
    public class Crawler
    {
        private readonly Dictionary<string, string> _defaultHeaders = new Dictionary<string, string>
        {
            {"Accept", "application/json"},
            {"UserAgent", "foo-bar"}
        };

        public async Task<(CrawledPage, List<NewPage>)> CrawlAsync(Uri uri)
        {
            var linksFound = new List<NewPage>();

            var crawledPage = new CrawledPage();

            using (IRestClient client = new RestClient(_defaultHeaders, timeout: 15.Seconds()))
            {
                using (var response = await client.SendAsync(new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = uri }))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return (crawledPage, linksFound);
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
                        if (result != null)
                        {
                            crawledPage.Content = result;
                            var document = new HtmlDocument();
                            document.LoadHtml(result);

                            var links = PageProcessor.GetAllLinks(document);

                            foreach (var link in links)
                            {
                                var page = new NewPage {
                                    Uri = link,
                                    Added = crawledPage.LastCheck.Value,
                                    FoundOn = crawledPage.Uri
                                };
                                linksFound.Add(page);
                            }
                        }
                    }
                }
            }
            return (crawledPage, linksFound);
        }
    }
}
