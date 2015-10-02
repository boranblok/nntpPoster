using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Util.Configuration;

namespace nntpAutoposter
{
    public class IndexerNotifier
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Object monitor = new Object();
        private Settings configuration;
        private Task MyTask;
        private Boolean StopRequested;

        public IndexerNotifier(Settings configuration)
        {
            this.configuration = configuration;
            StopRequested = false;
            MyTask = new Task(IndexerNotifierTask, TaskCreationOptions.LongRunning);
        }

        public void Start()
        {
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
        }

        private void IndexerNotifierTask()
        {
            try
            {
                while (!StopRequested)
                {
                    NotifyIndexerOfNewObfuscatedUploads();
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

        private void NotifyIndexerOfNewObfuscatedUploads()
        {
            foreach(var upload in DBHandler.Instance.GetUploadEntriesToNotifyIndexer())
            {
                try
                {
                    NotifyIndexerOfObfuscatedUpload(upload);
                    upload.NotifiedIndexerAt = DateTime.UtcNow;
                    DBHandler.Instance.UpdateUploadEntry(upload);
                    log.InfoFormat("Notified indexer that obfuscated release [{0}] is actually [{1}]", 
                        upload.ObscuredName, upload.CleanedName);
                }
                catch(Exception ex)
                {
                    log.Error("Could not notify indexer of obfuscated release.", ex);
                }
            }
        }

        private void NotifyIndexerOfObfuscatedUpload(UploadEntry upload)
        {
            String notificationGetUrl = String.Format(
                configuration.ObfuscatedNotificationUrl, 
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

            using(var reader = new StreamReader(response.GetResponseStream()))
            {
                var responseBody = reader.ReadToEnd();
                if(responseBody.IndexOf("error") >= 0)
                    throw new Exception("Error when notifying indexer: " + responseBody);
            }           
        }

        private bool ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;    //HACK: this should be worked out better, right now we accept all SSL Certs.
        }
    }
}
