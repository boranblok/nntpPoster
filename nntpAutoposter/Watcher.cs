using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Util;

namespace nntpAutoposter
{
    class Watcher
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private AutoPosterConfig configuration;
        private FileSystemWatcher watcher;

        public Watcher(AutoPosterConfig configuration)
        {
            this.configuration = configuration;
            watcher = new FileSystemWatcher(configuration.WatchFolder.FullName);
            watcher.IncludeSubdirectories = false;
            watcher.Created += watcher_Created;
            watcher.Error += watcher_Error;

        }

        public void Start()
        {
            foreach (FileSystemInfo toPost in configuration.WatchFolder.EnumerateFileSystemInfos())
            {
                MoveToBackupFolderAndPost(toPost.FullName);
            }
            watcher.EnableRaisingEvents = true;
            log.InfoFormat("Monitoring '{0}' for new files or folders to post.", configuration.WatchFolder.FullName);
        }

        public void Stop()
        {
            watcher.EnableRaisingEvents = false;
        }

        private void watcher_Error(object sender, ErrorEventArgs e)
        {
            log.Fatal("Fatal exception in the filesystemwatcher.", e.GetException());
            Console.WriteLine("Fatal exception in the filesystemwatcher:");
            Console.WriteLine(e.GetException().ToString());
            Environment.Exit(1);
        }

        private void watcher_Created(object sender, FileSystemEventArgs e)
        {
            MoveToBackupFolderAndPost(e.FullPath);
        }

        private void MoveToBackupFolderAndPost(String fullPath)
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

            AddItemToPostingDb(backup);
        }

        private FileSystemInfo MoveFolderToBackup(String fullPath)
        {
            FileSystemInfo backup;
            DirectoryInfo toPost = new DirectoryInfo(fullPath);
            String destinationFolder = Path.Combine(configuration.BackupFolder.FullName, toPost.Name);
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

        private FileSystemInfo MoveFileToBackup(String fullPath)
        {
            FileSystemInfo backup;
            FileInfo toPost = new FileInfo(fullPath);
            String destinationFile = Path.Combine(configuration.BackupFolder.FullName, toPost.Name);
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
                    if (ex.HResult == -2147024864)  //Only handle file locked events.
                        Thread.Sleep(500);  //Sleep for half a second before retry.
                    else
                        throw;
                }
            }
            return backup;
        }

        private void AddItemToPostingDb(FileSystemInfo toPost)
        {
            UploadEntry newUploadentry = new UploadEntry();
            newUploadentry.CreatedAt = DateTime.UtcNow;
            newUploadentry.Name = toPost.Name;
            newUploadentry.RemoveAfterVerify = configuration.RemoveAfterVerify;
            newUploadentry.Cancelled = false;
            newUploadentry.Size = toPost.Size();
            DBHandler.Instance.AddNewUploadEntry(newUploadentry);
        }
    }
}
