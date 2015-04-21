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
            FileToPost toPost = new FileToPost(file);

            nntpMessagePoster poster = new nntpMessagePoster();
            PostedFileInfo postInfo = toPost.PostYEncFile(poster, "1/1", "");
            poster.WaitTillCompletion();
            XDocument nzbDoc = GenerateNzbFromPostInfo(toPost.FileName, new List<PostedFileInfo>(new PostedFileInfo[] { postInfo }));
            nzbDoc.Save(toPost.FileName + ".nzb");

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

            return 0;
        }

        private static XDocument GenerateNzbFromPostInfo(String title, List<PostedFileInfo> postedFiles)
        {
            XNamespace ns = "http://www.newzbin.com/DTD/2003/nzb";

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XDocumentType("nzb", "-//newzBin//DTD NZB 1.1//EN", "http://www.newzbin.com/DTD/nzb/nzb-1.1.dtd", null),
                new XElement(ns + "nzb",
                    new XElement(ns + "head",
                        new XElement(ns + "meta",
                            new XAttribute("type", "title"),
                            title
                        )
                    ),
                    postedFiles.Select(f =>
                        new XElement(ns + "file",
                            new XAttribute("poster", PostSettings.FromAddress),
                            new XAttribute("date", f.PostedDateTime.GetUnixTimeStamp()),
                            new XAttribute("subject", f.NzbSubjectName),
                            new XElement(ns + "groups",
                                f.PostedGroups.Select(g => new XElement(ns + "group", g))
                            ),
                            new XElement(ns + "segments",
                                f.Segments.OrderBy(s => s.SegmentNumber).Select(s =>
                                    new XElement(ns + "segment",
                                        new XAttribute("bytes", s.Bytes),
                                        new XAttribute("number", s.SegmentNumber),
                                        s.MessageIdWithoutBrackets
                                    )
                                )
                            )
                        )
                    )
                )
            );

            return doc;
        }
    }
}
