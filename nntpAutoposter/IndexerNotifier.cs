using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace nntpAutoposter
{
    class IndexerNotifier
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            try
            {
                while (!StopRequested)
                {
                    NotifyIndexerOfNewObscufatedUploads();
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
            catch(Exception ex)
            {
                log.Error("Serious error in the notifier thread.", ex);
            }
        }

        private void NotifyIndexerOfNewObscufatedUploads()
        {
            foreach(var upload in DBHandler.Instance.GetUploadEntriesToNotifyIndexer())
            {
                try
                {
                    NotifyIndexerOfObscufatedUpload(upload);
                    upload.NotifiedIndexerAt = DateTime.UtcNow;
                    DBHandler.Instance.UpdateUploadEntry(upload);
                    log.InfoFormat("Notified indexer that obscufated release [{0}] is actually [{1}]", 
                        upload.ObscuredName, upload.CleanedName);
                }
                catch(Exception ex)
                {
                    log.Error("Could not notify indexer of obscufated release.", ex);
                }
            }
        }

        private void NotifyIndexerOfObscufatedUpload(UploadEntry upload)
        {
            String notificationGetUrl = String.Format(
                configuration.ObscufatedNotificationUrl, 
                Uri.EscapeDataString(upload.ObscuredName), 
                Uri.EscapeDataString(upload.CleanedName));

            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;

            HttpWebRequest request = WebRequest.Create(notificationGetUrl) as HttpWebRequest;       //Mono does not support CreateHttp
            //request.ServerCertificateValidationCallback = ServerCertificateValidationCallback;    //Not implemented in mono
            request.Method = "GET";
            request.Timeout = 60 * 1000;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("Error when notifying indexer: "
                                + response.StatusCode + " " + response.StatusDescription);
        }

        private bool ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;    //HACK: this should be worked out better, right now we accept all SSL Certs.
        }
    }
}
