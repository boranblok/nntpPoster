using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NntpClientLib;

namespace nntpPoster
{
    class Program
    {    
        static Int32 Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Please supply a filename to upload.");
                return 1;
            }
            FileInfo file = new FileInfo(args[0]);
            if (!file.Exists)
            {
                Console.WriteLine("The supplied file does not exist.");
                return 2;
            }
            UsenetPosterConfig config = new UsenetPosterConfig();
            UsenetPoster poster = new UsenetPoster(config);
            poster.newUploadSpeedReport += poster_newUploadSpeedReport;
            poster.PostToUsenet(file);

            //using (Rfc977NntpClientWithExtensions client = new Rfc977NntpClientWithExtensions())
            //{
            //    client.ProtocolLogger = Console.Out;

            //    client.Connect(PostSettings.NewsGroupAddress, PostSettings.NewsGroupUseSsl);
            //    client.AuthenticateUser(PostSettings.NewsGroupUsername, PostSettings.NewsGroupPassword);

            //    string newsgroup = "alt.test";
            //    client.SelectNewsgroup(newsgroup);

            //    ArticleHeadersDictionary headers = new ArticleHeadersDictionary();
            //    headers.AddHeader("From", PostSettings.FromAddress);
            //    headers.AddHeader("Subject", "Test - " + Guid.NewGuid().ToString());
            //    headers.AddHeader("Newsgroups", newsgroup);
            //    NntpDateTime dateTime = new NntpDateTime(DateTime.Now);
            //    headers.AddHeader("Date", dateTime.ToString());

            //    List<String> body = new List<String>();
            //    body.Add("A single line message to test retrieval of message ID");

            //    Console.WriteLine(client.PostArticle(new ArticleHeadersDictionaryEnumerator(headers), body));
            //}

#if DEBUG       //VS does not halt after execution in debug mode.
            Console.WriteLine("Finished");
            Console.ReadKey();
#endif

            return 0;
        }

        static void poster_newUploadSpeedReport(object sender, UploadSpeedReport e)
        {
            Console.Write("\r" + e.ToString() + "          ");
        }
    }
}
