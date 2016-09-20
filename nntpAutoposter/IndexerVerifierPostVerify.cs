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

        protected override Boolean UploadIsOnIndexer(UploadEntry upload)
        {
            String verificationGetUrl = String.Format(Configuration.SearchUrl, upload.ObscuredName);

            ServicePointManager.ServerCertificateValidationCallback = ServerCertificateValidationCallback;

            HttpWebRequest request = WebRequest.Create(verificationGetUrl) as HttpWebRequest;       //Mono does not support CreateHttp
            //request.ServerCertificateValidationCallback = ServerCertificateValidationCallback;    //Not implemented in mono
            request.Method = "GET";
            request.Timeout = 60*1000;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;            

            using(var reader = new StreamReader(response.GetResponseStream()))
            {
                var responseBody = reader.ReadToEnd();

                if (response.StatusCode == HttpStatusCode.OK)
                    return true;
                else
                    throw new Exception("Error when verifying on indexer: " + response.StatusCode + " " + response.StatusDescription + " " + responseBody);               
            }
        }

        private bool ServerCertificateValidationCallback(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;    //HACK: this should be worked out better, right now we accept all SSL Certs.
        }
    }
}
