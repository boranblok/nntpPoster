using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Util.Configuration;

namespace nntpAutoposter
{
    public class IndexerNotifierNzbPost : IndexerNotifierBase
    {
        internal IndexerNotifierNzbPost(Settings configuration) : base(configuration)
        {
        }

        protected override void NotifyIndexerOfObfuscatedUpload(UploadEntry upload)
        {
            String notificationUrl = String.Format(Configuration.ObfuscatedNotificationUrl);

            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;

            Byte[] nzbFileArray = UTF8Encoding.Default.GetBytes(upload.NzbContents);

            HttpClient httpClient = new HttpClient();

            MultipartFormDataContent form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(nzbFileArray), "file", upload.CleanedName + ".nzb");
            Task<HttpResponseMessage> responseTask = httpClient.PostAsync(notificationUrl, form);
            responseTask.Wait();
            HttpResponseMessage response = responseTask.Result;

            HttpContent content = response.Content;
            Task<String> contentStringTask = content.ReadAsStringAsync();
            contentStringTask.Wait();
            String contentString = contentStringTask.Result;

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("Error when notifying indexer: "
                    + response.StatusCode + " " + response.ReasonPhrase + " " + contentString);     
        }

        private bool ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;    //HACK: this should be worked out better, right now we accept all SSL Certs.
        }
    }
}
