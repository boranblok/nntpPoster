using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using nntpPoster;

namespace nntpAutoposter
{    
    class AutoPoster
    {
        private AutoPosterConfig configuration;
        private UsenetPosterConfig posterConfiguration;
        private UsenetPoster poster;
        private Task MyTask;
        private Boolean StopRequested;

        public AutoPoster(AutoPosterConfig configuration)
        {
            this.configuration = configuration;
            posterConfiguration = new UsenetPosterConfig();
            poster = new UsenetPoster(posterConfiguration);
            StopRequested = false;
            MyTask = new Task(AutopostingTask, TaskCreationOptions.LongRunning);
        }

        public void Start()
        {
            MyTask.Start();
        }

        public void Stop()
        {
            StopRequested = true;
            MyTask.Wait();
        }

        private void AutopostingTask()
        {
            while(!StopRequested)
            {
                UploadNextItemInQueue();
                if(!StopRequested)
                    Thread.Sleep(configuration.AutoposterIntervalMillis);
            }
        }

        private void UploadNextItemInQueue()
        {
            UploadEntry nextItemInQueue = DBHandler.Instance.GetNextUploadEntryToUpload();
            if (nextItemInQueue != null)
            {
                FileSystemInfo toPost;
                String fullPath = Path.Combine(configuration.BackupFolder.FullName, nextItemInQueue.Name);
                FileAttributes attributes = File.GetAttributes(fullPath);
                if (attributes.HasFlag(FileAttributes.Directory))
                    toPost = new DirectoryInfo(fullPath);
                else
                    toPost = new FileInfo(fullPath);

                poster.PostToUsenet(toPost);

                nextItemInQueue.UploadedAt = DateTime.UtcNow;
                DBHandler.Instance.UpdateUploadEntry(nextItemInQueue);
            }
        }
    }
}
