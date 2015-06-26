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
                    List<ExtendedFileInfo> filesToProcess = new List<ExtendedFileInfo>();
                    foreach (FileSystemInfo toRename in configuration.RootWatchFolder.EnumerateFileSystemInfos())
                    {
                        filesToProcess.AddRange(GetExtendedFileInfo(toRename));
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
                    Monitor.Wait(monitor, configuration.WatchFolderCheckIntervalSeconds * 1000);
                }
            }
        }

        public void Start()
        {
            log.InfoFormat("Monitoring '{0}' for new files or folders to rename.", configuration.RootWatchFolder.FullName);      
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
            log.InfoFormat("Monitoring '{0}' for files and folders stopped.", configuration.RootWatchFolder.FullName);
        }

        private List<ExtendedFileInfo> GetExtendedFileInfo(FileSystemInfo toProcess)
        {
            List<ExtendedFileInfo> filesToProcess = new List<ExtendedFileInfo>();
            FileAttributes attributes = File.GetAttributes(toProcess.FullName);
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                filesToProcess.AddRange(GetExtendedFileInfo(new DirectoryInfo(toProcess.FullName)));
            }
            else
            {
                filesToProcess.Add(GetExtendedFileInfo(new FileInfo(toProcess.FullName)));
            }
            return filesToProcess;
        }

        private List<ExtendedFileInfo> GetExtendedFileInfo(DirectoryInfo directoryInfo)
        {
            List<ExtendedFileInfo> filesToProcess = new List<ExtendedFileInfo>();
            if (configuration.TagFolders.Contains(directoryInfo.Name))
            {
                foreach (FileSystemInfo toRename in directoryInfo.EnumerateFileSystemInfos())
                {
                    filesToProcess.AddRange(GetExtendedFileInfo(toRename));
                }
            }
            else
            {
                foreach (FileInfo file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    filesToProcess.Add(GetExtendedFileInfo(file));
                }
            }
            return filesToProcess;
        }

        private ExtendedFileInfo GetExtendedFileInfo(FileInfo fileInfo)
        {
            FileType type = DetectFileType(fileInfo);

            return new ExtendedFileInfo
            {
                FileInfo = fileInfo,
                FileType = type
            };
        }

        private FileType DetectFileType(FileInfo fileInfo)
        {
            throw new NotImplementedException();
        }

        private void MoveFileToNoMatch(FileInfo fileInfo)
        {
            String destination = Path.Combine(configuration.NoMatchFolder.FullName,
                configuration.RootWatchFolder.GetRelativePath(fileInfo));
            File.Move(fileInfo.FullName, destination);
        }

        private void MoveFileToUnhanded(FileInfo fileInfo)
        {
            String destination = Path.Combine(configuration.UnhandledFilesFolder.FullName,
                configuration.RootWatchFolder.GetRelativePath(fileInfo));
            File.Move(fileInfo.FullName, destination);
        }
    }
}
