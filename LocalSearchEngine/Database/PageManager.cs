using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LiteDB;
using LocalSearchEngine.Database.Models;

namespace LocalSearchEngine.Database
{
    public class PageManager : IDisposable
    {
        private static string _workingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Data";

        private readonly LiteDatabase _db;

        private LiteCollection<Page> _pages;

        private LiteCollection<Link> _links;

        public PageManager(bool removeDataDirectory = false)
        {
            if (removeDataDirectory && Directory.Exists(_workingDir))
            {
                Directory.Delete(_workingDir, true);
            }

            Directory.CreateDirectory(_workingDir);
            _db = new LiteDatabase($"{_workingDir}/LSE.db");

            _pages = _db.GetCollection<Page>();
            _pages.EnsureIndex(x => x.Uri);

            _links = _db.GetCollection<Link>();
            _links.EnsureIndex(x => x.Added);
        }

        public void AddCrawledPage(Page page)
        {
            _pages.Upsert(page);
        }

        public void AddCrawledPages(IEnumerable<Page> pages)
        {
            _pages.Upsert(pages);
        }

        public void AddNewPage(Link page)
        {
            _links.Upsert(page);
        }

        public void AddNewPages(IEnumerable<Link> pages)
        {
            _links.Upsert(pages);
        }

        public Link NextToCrawl()
        {
            var oldest = _links.Min("Added");
            if (oldest != null)
            {
                return _links.FindOne(x => x.Added == oldest.AsDateTime);
            }
            return null;
        }

        public void RemoveNewPage(Link page)
        {
            _links.Delete(page.Id);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
