using System;
using System.IO;
using System.Net;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Util;

namespace nntpAutoposter
{
    class IndexerVerifier
    {
        private readonly Object _monitor = new Object();
        private readonly AutoPosterConfig _configuration;
        private readonly Task _myTask;
        private Boolean _stopRequested;

        public IndexerVerifier(AutoPosterConfig configuration)
        {
            _configuration = configuration;
            _stopRequested = false;
            _myTask = new Task(IndexerVerifierTask, TaskCreationOptions.LongRunning);
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

        private void IndexerVerifierTask()
        {
            while (!_stopRequested)
            {
                VerifyUploadsOnIndexer();
                lock (_monitor)
                {
                    if (_stopRequested)
                    {
                        break;
                    }
                    Monitor.Wait(_monitor, _configuration.VerifierIntervalMinutes * 60 * 1000);
                }
            }
        }

        private void VerifyUploadsOnIndexer()
        {
            foreach (var upload in DbHandler.Instance.GetUploadEntriesToVerify())
            {
                try
                {
                    Console.WriteLine("Checking if [{0}] has been indexed.", upload.CleanedName);
                    var fullPath = Path.Combine(_configuration.BackupFolder.FullName, upload.Name);

                    var backupExists = Directory.Exists(fullPath);
                    if(!backupExists)
                        backupExists = File.Exists(fullPath);

                    if (!backupExists)
                    {
                        Console.WriteLine("The upload [{0}] was removed from the backup folder, cancelling verification.", upload.Name);
                        upload.Cancelled = true;
                        DbHandler.Instance.UpdateUploadEntry(upload);
                        continue;
                    }

                    if ((DateTime.UtcNow - upload.UploadedAt.Value).TotalMinutes < _configuration.MinRepostAgeMinutes)
                    {
                        Console.WriteLine("The upload [{0}] is not older than {1} minutes. Skipping check for now.", 
                            upload.CleanedName, _configuration.MinRepostAgeMinutes);
                        continue;
                    }

                    if (UploadIsOnIndexer(upload))
                    {
                        upload.SeenOnIndexAt = DateTime.UtcNow;
                        DbHandler.Instance.UpdateUploadEntry(upload);
                        Console.WriteLine("Release [{0}] has been found on the indexer.", upload.CleanedName);

                        if (upload.RemoveAfterVerify)
                        {
                            var attributes = File.GetAttributes(fullPath);
                            if (attributes.HasFlag(FileAttributes.Directory))
                                Directory.Delete(fullPath, true);
                            else
                                File.Delete(fullPath);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Release [{0}] has NOT been found on the indexer. Checking if a repost is required.", 
                            upload.CleanedName);
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

        private Boolean UploadIsOnIndexer(UploadEntry upload)
        {
            var postAge = (Int32)Math.Ceiling((DateTime.UtcNow - upload.UploadedAt.Value).TotalDays);
            var verificationGetUrl = String.Format(
                _configuration.SearchUrl,
                Uri.EscapeDataString(upload.CleanedName),
                postAge);

            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;

            var request = WebRequest.Create(verificationGetUrl) as HttpWebRequest;       //Mono does not support CreateHttp
            //request.ServerCertificateValidationCallback = ServerCertificateValidationCallback;    //Not implemented in mono
            request.Method = "GET";
            request.Timeout = 60*1000;
            var response = request.GetResponse() as HttpWebResponse;
            if(response.StatusCode != HttpStatusCode.OK)
                throw new Exception("Error when verifying on indexer: "
                                + response.StatusCode + " " + response.StatusDescription);

            using (var xmlReader = XmlReader.Create(response.GetResponseStream()))
            {
                var feed = SyndicationFeed.Load(xmlReader);
                foreach (var item in feed.Items)
                {
                    var similarityPercentage =
                        LevenshteinDistance.SimilarityPercentage(item.Title.Text, upload.CleanedName);
                    if (similarityPercentage > _configuration.VerifySimilarityPercentageTreshold)
                        return true;
                }
            }
            return false;
        }

        private bool ServerCertificateValidationCallback(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;    //HACK: this should be worked out better, right now we accept all SSL Certs.
        }

        private void RepostIfRequired(UploadEntry upload)
        {
            var ageInMinutes = (DateTime.UtcNow - upload.UploadedAt.Value).TotalMinutes;
            var repostTreshold = Math.Pow(upload.Size, (1 / 2.45)) / 60; 
            //This is a bit of guesswork, a 15 MB item will repost after about 15 minutes, 
            // a  5 GB item will repost after about 2h30.
            // a 15 GB item will repost after about 4h00.
            // a 50 GB item will repost after about 6h30.
            
            //In any case, it gets overruled by the configuration here.
            if (repostTreshold < _configuration.MinRepostAgeMinutes)
                repostTreshold = _configuration.MinRepostAgeMinutes;
            if (repostTreshold > _configuration.MaxRepostAgeMinutes)
                repostTreshold = _configuration.MaxRepostAgeMinutes;

            if(ageInMinutes > repostTreshold)
            {
                Console.WriteLine("Could not find [{0}] after {1:F2} minutes, reposting.", upload.Name, repostTreshold);
                var repost = new UploadEntry();
                repost.Name = upload.Name;
                repost.RemoveAfterVerify = upload.RemoveAfterVerify;
                repost.Cancelled = false;
                repost.Size = upload.Size;
                DbHandler.Instance.AddNewUploadEntry(repost);   
                //This implicitly cancels all other uploads with the same name so no need to update the upload itself.
            }
        }
    }
}
