using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace nntpAutoposter
{
    class IndexerNotifier
    {
        private readonly Object _monitor = new Object();
        private readonly AutoPosterConfig _configuration;
        private readonly Task _myTask;
        private Boolean _stopRequested;

        public IndexerNotifier(AutoPosterConfig configuration)
        {
            _configuration = configuration;
            _stopRequested = false;
            _myTask = new Task(IndexerNotifierTask, TaskCreationOptions.LongRunning);
        }

        public void Start()
        {
            _myTask.Start();
        }

        public void Stop()
        {
            lock (_monitor)
            {
                _stopRequested = true;
                Monitor.Pulse(_monitor);
            }
            _myTask.Wait();
        }

        private void IndexerNotifierTask()
        {
            while (!_stopRequested)
            {
                NotifyIndexerOfNewObscufatedUploads();
                lock (_monitor)
                {
                    if (_stopRequested)
                    {
                        break;
                    }
                    Monitor.Wait(_monitor, _configuration.NotifierIntervalMinutes * 60 * 1000);
                }
            }
        }

        private void NotifyIndexerOfNewObscufatedUploads()
        {
            foreach(var upload in DbHandler.Instance.GetUploadEntriesToNotifyIndexer())
            {
                try
                {
                    NotifyIndexerOfObscufatedUpload(upload);
                    upload.NotifiedIndexerAt = DateTime.UtcNow;
                    DbHandler.Instance.UpdateUploadEntry(upload);
                    Console.WriteLine("Notified indexer that obscufated release [{0}] is actually [{1}]", 
                        upload.ObscuredName, upload.CleanedName);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Could not notify indexer of obscufated release:");
                    Console.WriteLine(ex.ToString());
                    //TODO: Log.
                }
            }
        }

        private void NotifyIndexerOfObscufatedUpload(UploadEntry upload)
        {
            var notificationGetUrl = String.Format(
                _configuration.ObscufatedNotificationUrl, 
                Uri.EscapeDataString(upload.ObscuredName), 
                Uri.EscapeDataString(upload.CleanedName));

            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;

            var request = WebRequest.Create(notificationGetUrl) as HttpWebRequest;       //Mono does not support CreateHttp
            //request.ServerCertificateValidationCallback = ServerCertificateValidationCallback;    //Not implemented in mono
            request.Method = "GET";
            request.Timeout = 60 * 1000;
            var response = request.GetResponse() as HttpWebResponse;
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
