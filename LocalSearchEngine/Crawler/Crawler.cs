using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Easy.Common;
using Easy.Common.Extensions;
using Easy.Common.Interfaces;
using HtmlAgilityPack;
using LocalSearchEngine.Database.Models;

namespace LocalSearchEngine.Crawler
{
    public class Crawler
    {
        private readonly Dictionary<string, string> _defaultHeaders = new Dictionary<string, string>
        {
            {"Accept", "application/json"},
            {"UserAgent", "lse"}
        };

        public async Task<(Page, List<Link>)> CrawlAsync(Uri uri)
        {
            var linksFound = new List<Link>();

            var crawledPage = new Page();

            try
            {
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

                                foreach (var newUri in links)
                                {
                                    var link = new Link
                                    {
                                        Uri = newUri,
                                        Added = crawledPage.LastCheck.Value,
                                        FoundOn = crawledPage.Uri
                                    };
                                    linksFound.Add(link);
                                }

                                var metadata = PageProcessor.ExtractMetadata(crawledPage.Content, uri);
                                crawledPage.InsertMetadata(metadata);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return (crawledPage, linksFound);
        }
    }
}
