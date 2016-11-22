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
    public class IndexerVerifierPostVerify : IndexerVerifierBase
    {
        internal IndexerVerifierPostVerify(Settings configuration) : base(configuration)
        {
        }

        protected override void VerifyEntryOnIndexer(UploadEntry upload)
        {
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
        }

        protected override Boolean UploadIsOnIndexer(UploadEntry upload)
        {
            String verificationGetUrl = String.Format(Configuration.SearchUrl, upload.ObscuredName);

            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;

            HttpWebRequest request = WebRequest.Create(verificationGetUrl) as HttpWebRequest;       //Mono does not support CreateHttp
            //request.ServerCertificateValidationCallback = ServerCertificateValidationCallback;    //Not implemented in mono
            request.Method = "GET";
            request.Timeout = 60*1000;
            HttpWebResponse response;

            try
            {
                response = request.GetResponse() as HttpWebResponse;
            }
            catch(WebException ex)
            {
                response = ex.Response as HttpWebResponse;
            }

            using(var reader = new StreamReader(response.GetResponseStream()))
            {
                var responseBody = reader.ReadToEnd();

                switch(response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        log.InfoFormat("The release {0} was found on indexer. Response: {1}", upload.CleanedName, responseBody);
                        return true;
                    case HttpStatusCode.NotFound:
                        log.InfoFormat("The release {0} was NOT found on indexer. Response: {1}", upload.CleanedName, responseBody);
                        RepostIfRequired(upload);
                        return false;
                    case HttpStatusCode.InternalServerError:
                        HandleServerError(upload, responseBody);
                        return false;
                    default:
                        throw new Exception("Error when verifying on indexer: " + response.StatusCode + " " + response.StatusDescription + " " + responseBody);
                }
            }
        }

        private void HandleServerError(UploadEntry upload, String responseBody)
        {
            if(responseBody.IndexOf("ALREADY EXISTS") >= 0)
            {
                log.InfoFormat("The release {0} already exists.", upload.CleanedName);
                if (upload.IsRepost)
                {
                    log.WarnFormat("This release was already a repost. Cancelling.");
                    upload.Cancelled = true;
                    upload.MoveToFailedFolder(Configuration);
                }
                else
                {
                    WatchFolderSettings wfConfig = Configuration.GetWatchFolderSettings(upload.WatchFolderShortName);
                    upload.CleanedName = upload.CleanedName.Remove(upload.CleanedName.Length - wfConfig.PostTag.Length - 1) + "-REPOST" + wfConfig.PostTag;
                    upload.IsRepost = true;
                    log.InfoFormat("Reuploading as {0}", upload.CleanedName);
                    upload.UploadedAt = null;
                }
            }
            else
            {
                log.WarnFormat("Fatal exception on the server side: {0}", responseBody);
                log.InfoFormat("Reposting {0}", upload.CleanedName);
                upload.UploadedAt = null;
            }           

            DBHandler.Instance.UpdateUploadEntry(upload);
        }

        private bool ServerCertificateValidationCallback(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;    //HACK: this should be worked out better, right now we accept all SSL Certs.
        }
    }
}
