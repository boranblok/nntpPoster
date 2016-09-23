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
                        return false;
                    default:
                        throw new Exception("Error when verifying on indexer: " + response.StatusCode + " " + response.StatusDescription + " " + responseBody);
                }
            }
        }

        private bool ServerCertificateValidationCallback(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;    //HACK: this should be worked out better, right now we accept all SSL Certs.
        }
    }
}
