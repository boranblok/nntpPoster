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
            if (upload.UploadedAt != null)
            {
                String notificationUrl = String.Format(Configuration.ObfuscatedNotificationUrl);

                ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;

                Byte[] nzbFileArray = UTF8Encoding.Default.GetBytes(upload.NzbContents);

                HttpClient httpClient = new HttpClient();

                MultipartFormDataContent form = new MultipartFormDataContent();
                form.Add(new ByteArrayContent(nzbFileArray), Configuration.NzbPostFilenameParam, upload.CleanedName + ".nzb");
                
                if(!String.IsNullOrWhiteSpace(Configuration.NzbPostExtraParams))
                {
                    var extraParams = Configuration.NzbPostExtraParams.Split(new Char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach(var extraParam in extraParams)
                    {
                        var keyAndValue = extraParam.Split(new Char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                        if(keyAndValue.Length != 2)
                            throw new Exception("Configuration error, NzbPostExtraParams is specified, but not in key=value&key2=value2 format.");
                        form.Add(new StringContent(keyAndValue[1]), keyAndValue[0]);
                    }
                }

                HttpResponseMessage response = httpClient.PostAsync(notificationUrl, form).Result;

                HttpContent content = response.Content;
                String contentString = content.ReadAsStringAsync().Result;

                if (response.StatusCode != HttpStatusCode.OK)
                    throw new Exception("Error when notifying indexer: "
                        + response.StatusCode + " " + response.ReasonPhrase + " " + contentString);
            }
        }

        private bool ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;    //HACK: this should be worked out better, right now we accept all SSL Certs.
        }
    }
}
