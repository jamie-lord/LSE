using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        }

        public void UpdatePage(Page page)
        {
            var e = _db.Find<Page>(x => x.Id == page.Id || x.Uri == page.Uri);
            if (e != null)
            {
                page.Id = e.Id;
                _db.Update(page);
            }
            else
            {
                _db.Insert(page);
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
                _db.Insert(link);
            }
        }

        public void UpdateLinks(IEnumerable<Link> links)
        {
            foreach (var link in links)
            {
                UpdateLink(link);
            }
        }

        public Link NextToCrawl()
        {
            return _db.Query<Link>("select * from Link order by Added").FirstOrDefault();
        }

        public void RemoveNewPage(Link page)
        {
            _db.Delete(page);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
