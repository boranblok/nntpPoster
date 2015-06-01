using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Util;

namespace VideoFileRenamer
{
    class Watcher
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Object monitor = new Object();
        private RenamerConfiguration configuration;
        private Task MyTask;
        private Boolean StopRequested;

        public Watcher(RenamerConfiguration configuration)
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
                    foreach (FileSystemInfo toRename in configuration.WatchFolder.EnumerateFileSystemInfos())
                    {
                        RenameFileSystemInfo(toRename);
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

        private void RenameFileSystemInfo(FileSystemInfo toRename)
        {
            FileAttributes attributes = File.GetAttributes(toRename.FullName);
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                RenameFilesInDirectory(new DirectoryInfo(toRename.FullName));
            }
            else
            {
                RenameFile(new FileInfo(toRename.FullName));
            }
        }

        private void RenameFilesInDirectory(DirectoryInfo directoryInfo)
        {
            foreach(FileInfo file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                RenameFile(file);
            }
        }

        private void RenameFile(FileInfo fileInfo)
        {
            if (!configuration.HandledFileExtensions.Contains(fileInfo.Extension))
            {
                MoveFileToUnhanded(fileInfo);
                return;
            }

        }

        private void MoveFileToUnhanded(FileInfo fileInfo)
        {
            String destination = Path.Combine(configuration.UnhandledFilesFolder.FullName,
                configuration.RootWatchFolder.GetRelativePath(fileInfo))
            File.Move(fileInfo.FullName, destination)
        }
    }
}
