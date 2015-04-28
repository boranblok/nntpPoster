using System;
using System.IO;
using rarLib;

namespace rarTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var source = new DirectoryInfo(@"/mnt/virtual/shareroot/ToSort/TestRarring/SourceFolder/");
            var target = new DirectoryInfo(@"/mnt/virtual/shareroot/ToSort/TestRarring/TargetFolder/");
            var targetName = "RarFilename";

            var rarWrapper = new RarWrapper();

            rarWrapper.Compress(source, target, targetName, 15*1000*1000);

#if DEBUG       //VS does not halt after execution in debug mode.
            Console.WriteLine("Finished");
            Console.ReadKey();
#endif
        }
    }
}
