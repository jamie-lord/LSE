using System;
using System.Collections.Generic;
using System.Linq;
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
        public Crawler()
        {
        }

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

                            if (document != null)
                            {
                                var links = document.DocumentNode.SelectNodes("//a[@href]");
                                if (links != null)
                                {
                                    foreach (HtmlNode link in links)
                                    {
                                        HtmlAttribute att = link.Attributes["href"];

                                        if (Uri.IsWellFormedUriString(att.Value, UriKind.Absolute))
                                        {
                                            var foundUri = new Uri(att.Value);
                                            if (!linksFound.Any(x => x.Uri == foundUri) && foundUri != uri)
                                            {
                                                linksFound.Add(new NewPage
                                                {
                                                    Uri = foundUri,
                                                    Added = crawledPage.LastCheck.Value,
                                                    FoundOn = crawledPage.Uri
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return (crawledPage, linksFound);
        }
    }
}
