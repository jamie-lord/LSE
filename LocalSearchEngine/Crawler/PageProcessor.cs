using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace LocalSearchEngine.Crawler
{
    public static class PageProcessor
    {
        public static List<Uri> GetAllLinks(HtmlDocument document)
        {
            var links = new List<Uri>();
            if (document == null)
            {
                return links;
            }

            var linkNodes = document.DocumentNode.SelectNodes("//a[@href]");
            if (linkNodes != null)
            {
                foreach (HtmlNode link in linkNodes)
                {
                    HtmlAttribute att = link.Attributes["href"];

                    if (Uri.IsWellFormedUriString(att.Value, UriKind.Absolute))
                    {
                        var foundUri = new Uri(att.Value);
                        if (!links.Contains(foundUri))
                        {
                            links.Add(foundUri);
                        }
                    }
                }
            }
            return links;
        }
    }
}
