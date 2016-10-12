using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using log4net;
using Util;
using Util.Configuration;

namespace nntpAutoposter
{
    public abstract class IndexerVerifierBase
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Object monitor = new Object();
        private Task MyTask;
        private Boolean StopRequested;

        protected Settings Configuration { get; set; }

        public static IndexerVerifierBase GetActiveVerifier(Settings configuration)
        {
            if ("NewznabSearch".Equals(configuration.VerificationType, StringComparison.InvariantCultureIgnoreCase))
            {
                return new IndexerVerifierNewznabSearch(configuration);
            }            

            if("PostVerify".Equals(configuration.VerificationType, StringComparison.InvariantCultureIgnoreCase))
            {
                return new IndexerVerifierPostVerify(configuration);
            }

            if("Dummy".Equals(configuration.VerificationType, StringComparison.InvariantCultureIgnoreCase))
            {
                return new IndexerVerifierDummy(configuration);
            }

            if (!String.IsNullOrEmpty(configuration.VerificationType))
                log.WarnFormat("{0} is an unknown verification type. Valid values are 'NewznabSearch', 'PostVerify' and 'Dummy'");
            else
                log.InfoFormat("No verification type defined in configuration.");
            
            if(configuration.BackupFolder != null)
                log.Warn("You will have to clean up the backup directory manually or your disk will fill up.");

            return null;
        }

        protected IndexerVerifierBase(Settings configuration)
        {
            this.Configuration = configuration;
            StopRequested = false;
            MyTask = new Task(IndexerVerifierTask, TaskCreationOptions.LongRunning);
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

        private void IndexerVerifierTask()
        {
            while (!StopRequested)
            {
                VerifyUploadsOnIndexer();
                lock (monitor)
                {
                    if (StopRequested)
                    {
                        break;
                    }
                    Monitor.Wait(monitor, Configuration.VerifierIntervalMinutes * 60 * 1000);
                }
            }
        }

        private void VerifyUploadsOnIndexer()
        {
            foreach (var upload in DBHandler.Instance.GetUploadEntriesToVerify())
            {
                try
                {
                    String fullPath = Path.Combine(Configuration.BackupFolder.FullName,
                        upload.WatchFolderShortName, upload.Name);
                    
                    Boolean backupExists = false;
                    backupExists = Directory.Exists(fullPath);
                    if(!backupExists)
                        backupExists = File.Exists(fullPath);

                    if (!backupExists)
                    {
                        log.WarnFormat("The upload [{0}] was removed from the backup folder, cancelling verification.", 
                            upload.Name);
                        upload.Cancelled = true;
                        DBHandler.Instance.UpdateUploadEntry(upload);
                        continue;
                    }

                    if ((DateTime.UtcNow - upload.UploadedAt.Value).TotalMinutes < Configuration.VerifyAfterMinutes)
                    {
                        log.DebugFormat("The upload [{0}] is younger than {1} minutes. Skipping check.", 
                            upload.CleanedName, Configuration.VerifyAfterMinutes);
                        continue;
                    }

                    if (UploadIsOnIndexer(upload))
                    {
                        upload.SeenOnIndexAt = DateTime.UtcNow;
                        DBHandler.Instance.UpdateUploadEntry(upload);
                        log.InfoFormat("Release [{0}] has been found on the indexer.", upload.CleanedName);

                        if (upload.RemoveAfterVerify)
                        {
                            upload.DeleteBackup(Configuration);
                        }
                    }
                    else
                    {
                        log.WarnFormat(
                            "Release [{0}] has NOT been found on the indexer. Checking if a repost is required.", 
                            upload.CleanedName);
                        RepostIfRequired(upload);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(String.Format("Could not verify release [{0}] on index:", upload.CleanedName), ex);
                }
            }
        }

        private void RepostIfRequired(UploadEntry upload)
        {
            var ageInMinutes = (DateTime.UtcNow - upload.UploadedAt.Value).TotalMinutes;

            if (ageInMinutes > Configuration.RepostAfterMinutes)
            {
                log.WarnFormat("Could not find [{0}] after {1} minutes, reposting, attempt {2}", upload.CleanedName, Configuration.RepostAfterMinutes, upload.UploadAttempts);
                upload.UploadedAt = null;

                DBHandler.Instance.UpdateUploadEntry(upload);

            }
            else
            {
                log.InfoFormat("A repost of [{0}] is not required as {1} minutes have not passed since upload.", upload.CleanedName, Configuration.RepostAfterMinutes);
            }           
        }

        protected abstract Boolean UploadIsOnIndexer(UploadEntry upload);
    }
}
