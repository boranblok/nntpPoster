using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nntpPoster;

namespace nntpAutoposter
{
    class Program
    {
        static UsenetPosterConfig config;
        static UsenetPoster poster;
        static void Main(string[] args)
        {
            config = new UsenetPosterConfig();
            poster = new UsenetPoster(config);
            poster.newUploadSpeedReport += poster_newUploadSpeedReport;

            DirectoryInfo watchFolder = new DirectoryInfo(ConfigurationManager.AppSettings["WatchFolder"]);
            if(!watchFolder.Exists)
                watchFolder.Create();
            foreach(FileSystemInfo toPost in watchFolder.EnumerateFileSystemInfos())
            {
                AutoPostItem(toPost);
            }
            FileSystemWatcher watcher = new FileSystemWatcher(watchFolder.FullName);
            watcher.Created += watcher_Created;
            watcher.Error += watcher_Error;
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Monitoring '{0}' for new files or folders to post press any key to stop.", 
                watchFolder.FullName);
            Console.ReadKey();
        }

        private static void poster_newUploadSpeedReport(object sender, UploadSpeedReport e)
        {
            Console.Write("\r" + e.ToString() + "          ");
        }

        static void watcher_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("The FileSystemWatcher got an error:");
            Console.WriteLine(e.GetException().ToString());
            Environment.Exit(1);
        }

        static void watcher_Created(object sender, FileSystemEventArgs e)
        {
            FileAttributes attributes = File.GetAttributes(e.FullPath);
            if (attributes.HasFlag(FileAttributes.Directory))
                AutoPostItem(new DirectoryInfo(e.FullPath));
            else
                AutoPostItem(new FileInfo(e.FullPath));            
        }

        private static void AutoPostItem(FileSystemInfo toPost)
        {
            Console.WriteLine("Autoposter is posting '{0}' to usenet", toPost.FullName);
            poster.PostToUsenet(toPost);
        }
    }
}
