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
    public abstract class IndexerNotifierBase
    {
        protected static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Object monitor = new Object();        
        private Task MyTask;
        private Boolean StopRequested;

        protected Settings Configuration { get; set; }

        public static IndexerNotifierBase GetActiveNotifier(Settings configuration)
        {
            if("NewznabHash".Equals(configuration.NotificationType, StringComparison.InvariantCultureIgnoreCase))
            {
                return new IndexerNotifierNewznabHash(configuration);
            }

            if("NzbPost".Equals(configuration.NotificationType, StringComparison.InvariantCultureIgnoreCase))
            {
                return new IndexerNotifierNzbPost(configuration);
            }

            if (!String.IsNullOrEmpty(configuration.NotificationType))
                log.WarnFormat("{0} is an unknown notification type. Valid values are 'NewznabHash' and 'NzbPost'");
            else
                log.InfoFormat("No notification type defined in configuration.");

            return null;
        }

        protected IndexerNotifierBase(Settings configuration)
        {
            Configuration = configuration;
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
                        Monitor.Wait(monitor, Configuration.NotifierIntervalMinutes * 60 * 1000);
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

        protected abstract void NotifyIndexerOfObfuscatedUpload(UploadEntry upload);
    }
}
