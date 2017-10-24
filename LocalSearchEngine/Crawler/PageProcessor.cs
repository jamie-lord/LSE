using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.Linq;
using SmartReader;
using Easy.Common.Extensions;

namespace LocalSearchEngine.Crawler
{
    public static class PageProcessor
    {
        public static List<string> GetAllLinks(HtmlDocument document)
        {
            var links = new List<string>();
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
                        if (!links.Contains(att.Value))
                        {
                            links.Add(att.Value);
                        }
                    }
                }
            }
            return links;
        }

        public static PageMetadata ExtractMetadata(HtmlDocument document, string uri)
        {
            var r = Reader.ParseArticle(uri, document.DocumentNode.OuterHtml);

            if (r.IsReadable)
            {
                return new PageMetadata(r.Author, r.Byline, r.Dir, r.Excerpt, r.Language, r.Length, r.PublicationDate, r.TimeToRead, r.Title, r.Uri.AbsoluteUri, r.TextContent);
            }
            else
            {
                return new PageMetadata { Title = FindTitle(document) };
            }
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

        public static List<Dictionary<string, string>> ExtractMetaTags(HtmlDocument document)
        {
            var metaTags = document.DocumentNode.SelectNodes("//meta");
            if (metaTags != null)
            {
                var tags = new List<Dictionary<string, string>>();
                foreach (var t in metaTags)
                {
                    var tagComponents = new Dictionary<string, string>();
                    foreach (var item in t.Attributes)
                    {
                        tagComponents.Add(item.Name, item.Value);
                    }
                    if (tagComponents.IsNotNullOrEmpty())
                    {
                        tags.Add(tagComponents);
                    }
                }

                if (tags.IsNotNullOrEmpty())
                {
                    return tags;
                }
            }
            return null;
        }
    }
}
