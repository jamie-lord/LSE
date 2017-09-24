using System.IO;
using System.Reflection;

namespace LocalSearchEngine
{
    class Program
    {
        private static readonly string _workingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        static void Main(string[] args)
        {
            var hoot = new Hoot.Hoot($"{_workingDir}/Indexes", "test", false);

            hoot.FreeMemory();

            hoot.Index(0, "this is some great text!");

            hoot.OptimizeIndex();
        }
    }
}
