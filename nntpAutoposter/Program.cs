using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using nntpPoster;

namespace nntpAutoposter
{
    class Program
    {
        static AutoPosterConfig autoPosterconfig;
        static UsenetPosterConfig posterConfig;
        static UsenetPoster poster;
        static void Main(string[] args)
        {
            autoPosterconfig = new AutoPosterConfig();
            posterConfig = new UsenetPosterConfig();
            
            poster = new UsenetPoster(posterConfig);
            poster.newUploadSpeedReport += poster_newUploadSpeedReport;

            foreach (FileSystemInfo toPost in autoPosterconfig.WatchFolder.EnumerateFileSystemInfos())
            {
                MoveToBackupFolderAndPost(toPost.FullName);
            }
            FileSystemWatcher watcher = new FileSystemWatcher(autoPosterconfig.WatchFolder.FullName);
            watcher.Created += watcher_Created;
            watcher.Error += watcher_Error;
            watcher.IncludeSubdirectories = false;
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Monitoring '{0}' for new files or folders to post press any key to stop.",
                autoPosterconfig.WatchFolder.FullName);
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
            MoveToBackupFolderAndPost(e.FullPath);
        }

        static void MoveToBackupFolderAndPost(String fullPath)
        {
            FileSystemInfo backup;
            FileAttributes attributes = File.GetAttributes(fullPath);
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                backup = MoveFolderToBackup(fullPath);
            }
            else
            {
                backup = MoveFileToBackup(fullPath);
            }

            AutoPostItem(backup);
        }

        private static FileSystemInfo MoveFolderToBackup(String fullPath)
        {
            FileSystemInfo backup;
            DirectoryInfo toPost = new DirectoryInfo(fullPath);
            String destinationFolder = Path.Combine(autoPosterconfig.BackupFolder.FullName, toPost.Name);
            backup = new DirectoryInfo(destinationFolder);
            if (backup.Exists)
                backup.Delete();
            Boolean retry = true;
            while (retry)
            {
                try
                {
                    Directory.Move(fullPath, destinationFolder);
                    retry = false;
                }
                catch (IOException ex)
                {
                    if (ex.HResult == -2147024891)  //Only handle file locked events.
                        Thread.Sleep(500);  //Sleep for half a second before retry.
                    else
                        throw;
                }
            }
            return backup;
        }

        private static FileSystemInfo MoveFileToBackup(String fullPath)
        {
            FileSystemInfo backup;
            FileInfo toPost = new FileInfo(fullPath);
            String destinationFile = Path.Combine(autoPosterconfig.BackupFolder.FullName, toPost.Name);
            backup = new FileInfo(destinationFile);
            if (backup.Exists)
                backup.Delete();
            Boolean retry = true;
            while (retry)
            {
                try
                {
                    File.Move(fullPath, destinationFile);
                    retry = false;
                }
                catch (IOException ex)
                {
                    if (ex.HResult == -2147024891)  //Only handle file locked events.
                        Thread.Sleep(500);  //Sleep for half a second before retry.
                    else
                        throw;
                }
            }
            return backup;
        }

        private static void AutoPostItem(FileSystemInfo toPost)
        {
            Console.WriteLine("Autoposter is posting '{0}' to usenet", toPost.FullName);



            poster.PostToUsenet(toPost);
        }
    }
}
