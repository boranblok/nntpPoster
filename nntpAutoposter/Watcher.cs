using System;
using System.IO;
using System.Threading;
using Util;

namespace nntpAutoposter
{
    class Watcher
    {
        private readonly AutoPosterConfig _configuration;
        private readonly FileSystemWatcher _watcher;

        public Watcher(AutoPosterConfig configuration)
        {
            _configuration = configuration;
            _watcher = new FileSystemWatcher(configuration.WatchFolder.FullName);
            _watcher.IncludeSubdirectories = false;
            _watcher.Created += watcher_Created;
            _watcher.Error += watcher_Error;

        }

        public void Start()
        {
            foreach (var toPost in _configuration.WatchFolder.EnumerateFileSystemInfos())
            {
                MoveToBackupFolderAndPost(toPost.FullName);
            }
            _watcher.EnableRaisingEvents = true;
            Console.WriteLine("Monitoring '{0}' for new files or folders to post.", _configuration.WatchFolder.FullName);
        }

        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
        }

        private void watcher_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("The FileSystemWatcher got an error:");
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
            var attributes = File.GetAttributes(fullPath);
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
            var toPost = new DirectoryInfo(fullPath);
            var destinationFolder = Path.Combine(_configuration.BackupFolder.FullName, toPost.Name);
            var backup = new DirectoryInfo(destinationFolder);
            if (backup.Exists)
                backup.Delete();
            var retry = true;
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
            var toPost = new FileInfo(fullPath);
            var destinationFile = Path.Combine(_configuration.BackupFolder.FullName, toPost.Name);
            FileSystemInfo backup = new FileInfo(destinationFile);
            if (backup.Exists)
                backup.Delete();
            var retry = true;
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
            var newUploadentry = new UploadEntry();
            newUploadentry.CreatedAt = DateTime.UtcNow;
            newUploadentry.Name = toPost.Name;
            newUploadentry.RemoveAfterVerify = _configuration.RemoveAfterVerify;
            newUploadentry.Cancelled = false;
            newUploadentry.Size = toPost.Size();
            DbHandler.Instance.AddNewUploadEntry(newUploadentry);
        }
    }
}
