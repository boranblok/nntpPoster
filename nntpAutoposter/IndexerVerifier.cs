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
    public class IndexerVerifier
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Object monitor = new Object();
        private Settings configuration;
        private Task MyTask;
        private Boolean StopRequested;

        public IndexerVerifier(Settings configuration)
        {
            this.configuration = configuration;
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
                    Monitor.Wait(monitor, configuration.VerifierIntervalMinutes * 60 * 1000);
                }
            }
        }

        private void VerifyUploadsOnIndexer()
        {
            foreach (var upload in DBHandler.Instance.GetUploadEntriesToVerify())
            {
                try
                {
                    String fullPath = Path.Combine(configuration.BackupFolder.FullName,
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

                    if ((DateTime.UtcNow - upload.UploadedAt.Value).TotalMinutes < configuration.VerifyAfterMinutes)
                    {
                        log.DebugFormat("The upload [{0}] is younger than {1} minutes. Skipping check.", 
                            upload.CleanedName, configuration.VerifyAfterMinutes);
                        continue;
                    }

                    if (UploadIsOnIndexer(upload))
                    {
                        upload.SeenOnIndexAt = DateTime.UtcNow;
                        DBHandler.Instance.UpdateUploadEntry(upload);
                        log.InfoFormat("Release [{0}] has been found on the indexer.", upload.CleanedName);

                        if (upload.RemoveAfterVerify)
                        {
                            FileAttributes attributes = File.GetAttributes(fullPath);
                            if (attributes.HasFlag(FileAttributes.Directory))
                                Directory.Delete(fullPath, true);
                            else
                                File.Delete(fullPath);
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

        private Boolean UploadIsOnIndexer(UploadEntry upload)
        {
            var postAge = (Int32)Math.Ceiling((DateTime.UtcNow - upload.UploadedAt.Value).TotalDays + 1);
            String searchName = GetIndexerSearchName(upload.CleanedName);
            String verificationGetUrl = String.Format(
                configuration.SearchUrl,
                Uri.EscapeDataString(searchName),
                postAge);

            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;

            HttpWebRequest request = WebRequest.Create(verificationGetUrl) as HttpWebRequest;       //Mono does not support CreateHttp
            //request.ServerCertificateValidationCallback = ServerCertificateValidationCallback;    //Not implemented in mono
            request.Method = "GET";
            request.Timeout = 60*1000;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            if(response.StatusCode != HttpStatusCode.OK)
                throw new Exception("Error when verifying on indexer: "
                                + response.StatusCode + " " + response.StatusDescription);

            using(var reader = new StreamReader(response.GetResponseStream()))
            {
                var responseBody = reader.ReadToEnd();
                if (responseBody.IndexOf("<error code=") >= 0)
                    throw new Exception("Error when verifying on indexer: " + responseBody);

                using (XmlReader xmlReader = XmlReader.Create(new StringReader(responseBody)))
                {
                    SyndicationFeed feed = SyndicationFeed.Load(xmlReader);
                    foreach (var item in feed.Items)
                    {
                        Decimal similarityPercentage =
                            LevenshteinDistance.SimilarityPercentage(CleanGeekBug(item.Title.Text), upload.CleanedName);
                        if (similarityPercentage > configuration.VerifySimilarityPercentageTreshold)
                            return true;

                        Decimal similarityPercentageWithIndexCleanedName =
                            LevenshteinDistance.SimilarityPercentage(CleanGeekBug(item.Title.Text), searchName);
                        if (similarityPercentageWithIndexCleanedName > configuration.VerifySimilarityPercentageTreshold)
                            return true;
                    }
                }
            }           
            return false;
        }

        //HACK: nzbgeek does a double escape of ampersands in the returned RSS feed. I dont think this is intended.
        private string CleanGeekBug(String title)
        {
            return title.Replace("&amp;amp;", "&");
        }

        private String GetIndexerSearchName(String cleanedName)
        {
            StringBuilder sb = new StringBuilder(cleanedName);
            if (String.IsNullOrEmpty(configuration.IndexerRenameMapSource))
                return cleanedName;

            for (int i = 0; i < configuration.IndexerRenameMapSource.Length; i++)
            {
                char source = configuration.IndexerRenameMapSource[i];
                char target = configuration.IndexerRenameMapTarget[i];
                if (source == target)
                {
                    sb.Replace(new string(new char[] {source}), String.Empty);
                }
                else
                {
                    sb.Replace(source, target);
                }
            }

            return sb.ToString();
        }

        private bool ServerCertificateValidationCallback(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;    //HACK: this should be worked out better, right now we accept all SSL Certs.
        }

        private void RepostIfRequired(UploadEntry upload)
        {
            var ageInMinutes = (DateTime.UtcNow - upload.UploadedAt.Value).TotalMinutes;

            if (ageInMinutes > configuration.RepostAfterMinutes)
            {
                log.WarnFormat("Could not find [{0}] after {1} minutes, reposting, attempt {2}", upload.CleanedName, configuration.RepostAfterMinutes, upload.UploadAttempts);
                upload.UploadedAt = null;

                DBHandler.Instance.UpdateUploadEntry(upload);

            }
            else
            {
                log.InfoFormat("A repost of [{0}] is not required as {1} minutes have not passed since upload.", upload.CleanedName, configuration.RepostAfterMinutes);
            }           
        }
    }
}
