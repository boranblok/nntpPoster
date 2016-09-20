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
    public class IndexerVerifierNewznabSearch : IndexerVerifierBase
    {
        internal IndexerVerifierNewznabSearch(Settings configuration) : base(configuration)
        {
        }

        protected override Boolean UploadIsOnIndexer(UploadEntry upload)
        {
            var postAge = (Int32)Math.Ceiling((DateTime.UtcNow - upload.UploadedAt.Value).TotalDays + 1);
            String searchName = GetIndexerSearchName(upload.CleanedName);
            String verificationGetUrl = String.Format(
                Configuration.SearchUrl,
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
                        if (similarityPercentage > Configuration.VerifySimilarityPercentageTreshold)
                            return true;

                        Decimal similarityPercentageWithIndexCleanedName =
                            LevenshteinDistance.SimilarityPercentage(CleanGeekBug(item.Title.Text), searchName);
                        if (similarityPercentageWithIndexCleanedName > Configuration.VerifySimilarityPercentageTreshold)
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
            if (String.IsNullOrEmpty(Configuration.IndexerRenameMapSource))
                return cleanedName;

            for (int i = 0; i < Configuration.IndexerRenameMapSource.Length; i++)
            {
                char source = Configuration.IndexerRenameMapSource[i];
                char target = Configuration.IndexerRenameMapTarget[i];
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
    }
}
