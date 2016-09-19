using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Util.Configuration;

namespace nntpAutoposter
{
    public class IndexerNotifierNewznabHash : IndexerNotifierBase
    {
        internal IndexerNotifierNewznabHash(Settings configuration) : base(configuration)
        {
        }

        protected override void NotifyIndexerOfObfuscatedUpload(UploadEntry upload)
        {
            String notificationGetUrl = String.Format(
                Configuration.ObfuscatedNotificationUrl, 
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
                if(responseBody.IndexOf("<error code=") >= 0)
                    throw new Exception("Error when notifying indexer: " + responseBody);
            }           
        }

        private bool ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;    //HACK: this should be worked out better, right now we accept all SSL Certs.
        }
    }
}
