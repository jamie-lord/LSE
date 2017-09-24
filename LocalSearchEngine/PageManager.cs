using System;
using System.IO;
using System.Reflection;
using LiteDB;

namespace LocalSearchEngine
{
    public class PageManager : IDisposable
    {
        private static string _workingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        private LiteDatabase _db = new LiteDatabase($"{_workingDir}/Pages.db");

        private LiteCollection<Page> _pages;

        public PageManager()
        {
            _pages = _db.GetCollection<Page>();
            _pages.EnsureIndex(x => x.Url);
        }

        public void AddPage(Page page)
        {
            _pages.Upsert(page);
        }

        public Page NextToCrawl()
        {
            return _pages.FindOne(x => x.LastCheck == null);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
