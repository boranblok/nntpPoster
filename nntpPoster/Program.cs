using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
