using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nntpAutoposter
{
    class IndexerVerifier
    {
        private AutoPosterConfig configuration;
        private Task MyTask;
        private Boolean StopRequested;

        public IndexerVerifier(AutoPosterConfig configuration)
        {
            this.configuration = configuration;
            StopRequested = false;
            MyTask = new Task(IndexerVerifierTask, TaskCreationOptions.LongRunning);
        }

        private void IndexerVerifierTask()
        {
            while (!StopRequested)
            {
                VerifyUploadsOnIndexer();
                if (!StopRequested)
                    Thread.Sleep(configuration.VerifierIntervalMinutes * 60 * 1000);
            }
        }

        private void VerifyUploadsOnIndexer()
        {
            foreach (var upload in DBHandler.Instance.GetUploadEntriesToVerify())
            {
                try
                {
                    if (FileIsOnIndexer(upload))
                    {
                        upload.SeenOnIndexAt = DateTime.UtcNow;
                        DBHandler.Instance.UpdateUploadEntry(upload);
                        Console.WriteLine("Release [{0}] has been found on the indexer.", upload.CleanedName);

                        if (upload.RemoveAfterVerify)
                        {
                            String fullPath = Path.Combine(configuration.BackupFolder.FullName, upload.Name);
                            FileAttributes attributes = File.GetAttributes(fullPath);
                            if (attributes.HasFlag(FileAttributes.Directory))
                                Directory.Delete(fullPath, true);
                            else
                                File.Delete(fullPath);
                        }
                    }
                    else
                    {
                        RepostIfRequired(upload);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not verify release [{0}] on index:", upload.CleanedName);
                    Console.WriteLine(ex.ToString());
                    //TODO: Log.
                }
            }
        }

        private Boolean FileIsOnIndexer(UploadEntry upload)
        {
            String notificationGetUrl = String.Format(
                configuration.SearchUrl,
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

                        return FindReleaseInResponse(getTask.Result);
                    }
                }
                finally
                {
                    if (getTask != null && getTask.Result != null)
                        getTask.Result.Dispose();
                }
            }
        }

        private Boolean FindReleaseInResponse(HttpResponseMessage httpResponseMessage)
        {
            throw new NotImplementedException();
        }

        private void RepostIfRequired(UploadEntry upload)
        {
            throw new NotImplementedException();
        }
    }
}
