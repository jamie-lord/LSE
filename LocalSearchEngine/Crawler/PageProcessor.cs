using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void ExtractMetadata(string pageContent)
        {
            var document = new HtmlDocument();
            document.LoadHtml(pageContent);

            if (document == null)
            {
                return;
            }
            var title = FindTitle(document);

        }

        private static string FindTitle(HtmlDocument document)
        {
            string title = document.DocumentNode.SelectSingleNode("//head/title").InnerText;
            if (!string.IsNullOrEmpty(title)) return title;
            title = document.DocumentNode.SelectSingleNode("//title").InnerText;
            if (!string.IsNullOrEmpty(title)) return title;
            title = document.DocumentNode.Descendants("title").FirstOrDefault().InnerText;
            return title;
        }
    }
}
