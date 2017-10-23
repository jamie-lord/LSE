using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.Linq;
using SmartReader;

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

        public static PageMetadata ExtractMetadata(string pageContent, Uri uri)
        {
            var r = Reader.ParseArticle(uri.AbsoluteUri, pageContent);

            if (r.IsReadable)
            {
                Console.WriteLine($"Author\t{r.Author}");
                Console.WriteLine($"By line\t{r.Byline}");
                Console.WriteLine($"Dir\t{r.Dir}");
                Console.WriteLine($"Excerpt\t{r.Excerpt}");
                Console.WriteLine($"Language\t{r.Language}");
                Console.WriteLine($"Length\t{r.Length}");
                Console.WriteLine($"Publication date\t{r.PublicationDate}");
                Console.WriteLine($"Time to read\t{r.TimeToRead}");
                Console.WriteLine($"Title\t{r.Title}");
                Console.WriteLine($"Uri\t{r.Uri}");
                Console.WriteLine($"Text content\t{r.TextContent}");
                return new PageMetadata(r.Author, r.Byline, r.Dir, r.Excerpt, r.Language, r.Length, r.PublicationDate, r.TimeToRead, r.Title, r.Uri, r.TextContent);
            }
            else
            {
                return new PageMetadata { Title = FindTitle(pageContent) };
            }
        }

        private static string FindTitle(string pageContent)
        {
            var document = new HtmlDocument();
            document.LoadHtml(pageContent);

            string title = document.DocumentNode.SelectSingleNode("//head/title").InnerText;
            if (!string.IsNullOrEmpty(title)) return title;
            title = document.DocumentNode.SelectSingleNode("//title").InnerText;
            if (!string.IsNullOrEmpty(title)) return title;
            title = document.DocumentNode.Descendants("title").FirstOrDefault().InnerText;
            return title;
        }
    }
}
