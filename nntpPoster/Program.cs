using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NntpClientLib;

namespace nntpPoster
{
    class Program
    {
        static void Main(string[] args)
        {
            nntpMessagePoster poster = new nntpMessagePoster();
            FileToPost toPost = new FileToPost(new FileInfo("testFiles\\BG2sizeComp.jpg"));
            PostedFileInfo postInfo = toPost.PostYEncFile(poster, "1/1", "");
            poster.WaitTillCompletion();
            XDocument nzbDoc = GenerateNzbFromPostInfo(toPost.FileName, new List<PostedFileInfo>(new PostedFileInfo[] { postInfo }));
            nzbDoc.Save(toPost.FileName + ".nzb");
            return;

            //using (Rfc977NntpClientWithExtensions client = new Rfc977NntpClientWithExtensions())
            //{
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

            //    client.PostArticle(new ArticleHeadersDictionaryEnumerator(headers), body);
            //}
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
