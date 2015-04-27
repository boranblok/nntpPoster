using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nntpAutoposter
{
    class IndexerNotifier
    {
        private Object monitor = new Object();
        private AutoPosterConfig configuration;
        private Task MyTask;
        private Boolean StopRequested;

        public IndexerNotifier(AutoPosterConfig configuration)
        {
            this.configuration = configuration;
            StopRequested = false;
            MyTask = new Task(IndexerNotifierTask, TaskCreationOptions.LongRunning);
        }

        public void Start()
        {
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
        }

        private void IndexerNotifierTask()
        {
            while (!StopRequested)
            {
                NotifyIndexerOfNewHashedUploads();
                lock (monitor)
                {
                    if (StopRequested)
                    {
                        break;
                    }
                    Monitor.Wait(monitor, configuration.NotifierIntervalMinutes * 60 * 1000);
                }
            }
        }

        private void NotifyIndexerOfNewHashedUploads()
        {
            foreach(var upload in DBHandler.Instance.GetUploadEntriesToNotifyIndexer())
            {
                try
                {
                    NotifyIndexerOfHashedUpload(upload);
                    upload.NotifiedIndexerAt = DateTime.UtcNow;
                    DBHandler.Instance.UpdateUploadEntry(upload);
                    Console.WriteLine("Notified indexer that hashed release [{0}] is actually [{1}]", 
                        upload.HashedName, upload.CleanedName);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Could not notify indexer of hashed release:");
                    Console.WriteLine(ex.ToString());
                    //TODO: Log.
                }
            }
        }

        private void NotifyIndexerOfHashedUpload(UploadEntry upload)
        {
            String notificationGetUrl = String.Format(
                configuration.HashedNotificationUrl, 
                Uri.EscapeDataString(upload.HashedName), 
                Uri.EscapeDataString(upload.CleanedName));
            using (HttpClient client = new HttpClient())
            {
                Task<HttpResponseMessage> getTask = null;
                try
                {
                    getTask = client.GetAsync(notificationGetUrl, HttpCompletionOption.ResponseContentRead);
                    getTask.Start();
                    getTask.Wait(60 * 1000);
                    if (getTask.IsCompleted)
                    {
                        if (getTask.IsFaulted)
                            throw getTask.Exception;
                        if (getTask.Result == null)
                            throw new Exception("No valid HttpResponse received.");

                        if (!getTask.Result.IsSuccessStatusCode)
                            throw new Exception("Error when notifying indexer: "
                                + getTask.Result.StatusCode + " " + getTask.Result.ReasonPhrase);
                    }
                }
                finally
                {
                    if (getTask != null && getTask.Result != null)
                        getTask.Result.Dispose();
                }
            }
        }
    }
}
