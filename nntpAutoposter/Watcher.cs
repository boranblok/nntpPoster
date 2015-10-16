using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Util;
using Util.Configuration;

namespace nntpAutoposter
{
    public class Watcher
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Object monitor = new Object();
        private Settings configuration;
        private Task MyTask;
        private Boolean StopRequested;

        public Watcher(Settings configuration)
        {
            this.configuration = configuration;
            StopRequested = false;
            MyTask = new Task(WatcherTask, TaskCreationOptions.LongRunning);
        }

        private void WatcherTask()
        {
            while (!StopRequested)
            {
                try
                {
                    foreach(WatchFolderSettings watchFolderSetting in configuration.WatchFolderSettings)
                        foreach (FileSystemInfo toPost in watchFolderSetting.Path.EnumerateFileSystemInfos())
                    {
                        MoveToBackupFolderAndPost(toPost, watchFolderSetting);
                    }
                }
                catch(Exception ex)
                {
                    log.Fatal("Fatal exception in the watcher task.", ex);
                    Environment.Exit(1);
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
            foreach (WatchFolderSettings watchFolderSetting in configuration.WatchFolderSettings)
                log.InfoFormat("Monitoring '{0}' for new files or folders to post.", watchFolderSetting.Path.FullName);      
            MyTask.Start();
        }

        public void Stop(Int32 millisecondsTimeout = Timeout.Infinite)
        {
            lock (monitor)
            {
                StopRequested = true;
                Monitor.Pulse(monitor);
            }
            MyTask.Wait(millisecondsTimeout);

            foreach (WatchFolderSettings watchFolderSetting in configuration.WatchFolderSettings)
                log.InfoFormat("Monitoring '{0}' for files and folders stopped.", watchFolderSetting.Path.FullName);
        }

        private void MoveToBackupFolderAndPost(FileSystemInfo toPost, WatchFolderSettings folderConfiguration)
        {
            try
            {
                if ((DateTime.Now - toPost.LastAccessTime).TotalMinutes > configuration.FilesystemCheckTesholdMinutes)
                {
                    DirectoryInfo destination = new DirectoryInfo(
                        Path.Combine(configuration.BackupFolder.FullName, folderConfiguration.ShortName));
                    FileSystemInfo backup = toPost.Move(destination);

                    AddItemToPostingDb(backup, folderConfiguration);
                }
            }
            catch(Exception ex)
            {
                log.Warn("Error when picking up a file/folder from the watch location.", ex);
            }
        }

        private void AddItemToPostingDb(FileSystemInfo toPost, WatchFolderSettings folderConfiguration)
        {
            UploadEntry newUploadentry = new UploadEntry();
            newUploadentry.WatchFolderShortName = folderConfiguration.ShortName;
            newUploadentry.CreatedAt = DateTime.UtcNow;
            newUploadentry.Name = toPost.Name;
            newUploadentry.RemoveAfterVerify = configuration.RemoveAfterVerify;
            newUploadentry.Cancelled = false;
            newUploadentry.Size = toPost.Size();
            if (newUploadentry.Size == 0)
            {
                log.ErrorFormat("File added with a size of 0 bytes, This cannot be uploaded! File name: [{0}]",
                    toPost.FullName);
                return;
            }
            DBHandler.Instance.AddNewUploadEntry(newUploadentry);
        }
    }
}
