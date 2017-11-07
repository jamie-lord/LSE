using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LocalSearchEngine.Database.Models;
using SQLite;

namespace LocalSearchEngine.Database
{
    public class PageManager : IDisposable
    {
        private static string _workingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Data";

        private readonly SQLiteConnection _db;

        public PageManager(bool removeDataDirectory = false)
        {
            if (removeDataDirectory && Directory.Exists(_workingDir))
            {
                Directory.Delete(_workingDir, true);
            }

            Directory.CreateDirectory(_workingDir);
            _db = new SQLiteConnection($"{_workingDir}/LSE.db", true);

            _db.CreateTable<Page>();
            _db.CreateTable<Link>();
            _db.CreateTable<PageContent>(CreateFlags.FullTextSearch4);
        }

        public void UpdatePage(Page page)
        {
            var e = _db.Find<Page>(x => x.Id == page.Id || x.Uri == page.Uri);
            if (e != null)
            {
                page.Id = e.Id;
                if (page.Content != null && !string.IsNullOrEmpty(page.Content.Content))
                {
                    page.Content.Id = page.Id;
                    var c = _db.Find<PageContent>(x => x.Id == page.Id);
                    if (c != null)
                    {
                        _db.Update(page.Content);
                    }
                    else
                    {
                        _db.Insert(page.Content);
                    }
                }
                _db.Update(page);
            }
            else
            {
                if (!string.IsNullOrEmpty(page.Uri))
                {
                    _db.Insert(page);
                    if (page.Content != null && !string.IsNullOrEmpty(page.Content.Content))
                    {
                        page.Content.Id = page.Id;
                        _db.Insert(page.Content);
                    }
                }
            }
        }

        public void UpdatePages(IEnumerable<Page> pages)
        {
            foreach (var page in pages)
            {
                UpdatePage(page);
            }
        }

        public void UpdateLink(Link link)
        {
            var e = _db.Find<Link>(x => x.Id == link.Id || x.Uri == link.Uri);
            if (e != null)
            {
                link.Id = e.Id;
                _db.Update(link);
            }
            else
            {
                if (!string.IsNullOrEmpty(link.Uri)) _db.Insert(link);
            }
        }

        public void UpdateLinks(IEnumerable<Link> links)
        {
            foreach (var link in links)
            {
                UpdateLink(link);
            }
        }

        private Queue<Link> _linksToCrawlBuffer = new Queue<Link>();
        private readonly object _linksToCrawlLock = new object();
        private const int _linkSelectLimit = 100;

        public Link NextToCrawl()
        {
            Link link = null;
            lock (_linksToCrawlLock)
            {
                if (_linksToCrawlBuffer.Count == 0)
                {
                    var r = _db.Query<Link>("SELECT * FROM Link ORDER BY Added LIMIT ?", _linkSelectLimit);
                    foreach (var l in r)
                    {
                        if (!_linksToCrawlBuffer.Contains(l))
                        {
                            _linksToCrawlBuffer.Enqueue(l);
                        }
                    }
                }
                else
                {
                    link = _linksToCrawlBuffer.Dequeue();
                }
            }
            return link;
        }

        public void RemoveLink(Link link)
        {
            _db.Delete(link);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
