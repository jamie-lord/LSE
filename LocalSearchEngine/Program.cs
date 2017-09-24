using System.IO;
using System.Reflection;

namespace LocalSearchEngine
{
    public class Program
    {
        private static readonly string _workingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public static void Main(string[] args)
        {
            //var hoot = new Hoot.Hoot($"{_workingDir}/Indexes", "test", false);

            //hoot.FreeMemory();

            //hoot.Index(0, "this is some great text!");

            //hoot.OptimizeIndex();

            var pm = new PageManager();
            pm.AddPage(new Page() {Url = "https://lord.technology"});

            var next = pm.NextToCrawl();
        }
    }
}
