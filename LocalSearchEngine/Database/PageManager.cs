using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LiteDB;

namespace LocalSearchEngine
{
    public class PageManager : IDisposable
    {
        private static string _workingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Data";

        private readonly LiteDatabase _db;

        private LiteCollection<CrawledPage> _crawledPages;

        private LiteCollection<NewPage> _newPages;

        public PageManager()
        {
            Directory.CreateDirectory(_workingDir);
            _db = new LiteDatabase($"{_workingDir}/LSE.db");

            _crawledPages = _db.GetCollection<CrawledPage>();
            _crawledPages.EnsureIndex(x => x.Uri);

            _newPages = _db.GetCollection<NewPage>();
            _newPages.EnsureIndex(x => x.Added);
        }

        public void AddCrawledPage(CrawledPage page)
        {
            _crawledPages.Upsert(page);
        }

        public void AddCrawledPages(IEnumerable<CrawledPage> pages)
        {
            _crawledPages.Upsert(pages);
        }

        public void AddNewPage(NewPage page)
        {
            _newPages.Upsert(page);
        }

        public void AddNewPages(IEnumerable<NewPage> pages)
        {
            _newPages.Upsert(pages);
        }

        public NewPage NextToCrawl()
        {
            var oldest = _newPages.Min("Added");
            if (oldest != null)
            {
                return _newPages.FindOne(x => x.Added == oldest.AsDateTime);
            }
            return null;
        }

        public void RemoveNewPage(NewPage page)
        {
            _newPages.Delete(page.Id);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
