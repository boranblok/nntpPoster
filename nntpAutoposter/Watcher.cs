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

        private Object monitor = new Object();
        private AutoPosterConfig configuration;
        private Task MyTask;
        private Boolean StopRequested;

        public Watcher(AutoPosterConfig configuration)
        {
            this.configuration = configuration;
            StopRequested = false;
            MyTask = new Task(WatcherTask, TaskCreationOptions.LongRunning);
        }

        private void WatcherTask()
        {
            while (!StopRequested)
            {
                foreach (FileSystemInfo toPost in configuration.WatchFolder.EnumerateFileSystemInfos())
                {
                    MoveToBackupFolderAndPost(toPost);
                }
                lock (monitor)
                {
                    if (StopRequested)
                    {
                        break;
                    }
                    Monitor.Wait(monitor, configuration.FilesystemCheckIntervalMillis);
                }
            }
        }

        public void Start()
        {
            log.InfoFormat("Monitoring '{0}' for new files or folders to post.", configuration.WatchFolder.FullName);      
            MyTask.Start();
        }

        public void Stop()
        {
            lock (monitor)
            {
                StopRequested = true;
                Monitor.Pulse(monitor);
            }
            MyTask.Wait();
            log.InfoFormat("Monitoring '{0}' for files and folders stopped.", configuration.WatchFolder.FullName);
        }

        private void MoveToBackupFolderAndPost(FileSystemInfo toPost)
        {
            if ((DateTime.Now - toPost.LastAccessTime).TotalMinutes > configuration.FilesystemCheckTesholdMinutes)
            {
                FileSystemInfo backup;
                FileAttributes attributes = File.GetAttributes(toPost.FullName);
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    backup = MoveFolderToBackup(toPost.FullName);
                }
                else
                {
                    backup = MoveFileToBackup(toPost.FullName);
                }

                AddItemToPostingDb(backup);
            }
        }

        private FileSystemInfo MoveFolderToBackup(String fullPath)
        {
            FileSystemInfo backup;
            DirectoryInfo toPost = new DirectoryInfo(fullPath);
            String destinationFolder = Path.Combine(configuration.BackupFolder.FullName, toPost.Name);
            backup = new DirectoryInfo(destinationFolder);
            if (backup.Exists)
            {
                log.WarnFormat("The backup folder for '{0}' already existed. Overwriting!", toPost.Name);
                backup.Delete();
            }
            Directory.Move(fullPath, destinationFolder);

            return backup;
        }

        private FileSystemInfo MoveFileToBackup(String fullPath)
        {
            FileSystemInfo backup;
            FileInfo toPost = new FileInfo(fullPath);
            String destinationFile = Path.Combine(configuration.BackupFolder.FullName, toPost.Name);
            backup = new FileInfo(destinationFile);
            if (backup.Exists)
            {
                log.WarnFormat("The backup folder for '{0}' already existed. Overwriting!", toPost.Name);
                backup.Delete();
            }

            File.Move(fullPath, destinationFile);
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
