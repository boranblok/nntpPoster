using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NntpClientLib;
using nntpPoster.yEncLib;

namespace nntpPoster
{
    public class nntpMessagePoster : InntpMessagePoster
    {
        private UsenetPosterConfig configuration;
        public nntpMessagePoster(UsenetPosterConfig configuration)
        {
            this.configuration = configuration;
        }
        private List<Task> RunningTasks = new List<Task>();
        public event EventHandler<YEncFilePart> FilePartPosted;

        protected virtual void OnFilePartPosted(YEncFilePart e)
        {
            if (FilePartPosted != null) FilePartPosted(this, e);
        }

        public void PostMessage(String subject, List<String> prefix, YEncFilePart yEncPart, List<String> suffix, PostedFileInfo postInfo)
        {
            Boolean waitForFreeThread = true;
            while(waitForFreeThread)
            {
                lock (RunningTasks)
                {
                    if (RunningTasks.Count < configuration.MaxConnectionCount)
                    {
                        Task task = new Task(() => PostMessageTask(subject, prefix, yEncPart, suffix, postInfo));
                        task.ContinueWith(t => CleanupTask(t));
                        RunningTasks.Add(task);
                        task.Start();
                        waitForFreeThread = false;
                    }
                }
                if(waitForFreeThread)
                    Thread.Sleep(2000);     //TODO: optimize sleep value here.
            }
        }

        public void WaitTillCompletion()
        {
            Task.WaitAll(RunningTasks.ToArray());
        }

        private void CleanupTask(Task task)
        {
            lock (RunningTasks)
            {
                RunningTasks.Remove(task);
            }
        }

        private void PostMessageTask(String subject, 
            List<String> prefix, YEncFilePart yEncPart, List<String> suffix, PostedFileInfo postInfo)
        {
            try
            {
                using (Rfc977NntpClientWithExtensions client = new Rfc977NntpClientWithExtensions())
                {
                    client.Connect(configuration.NewsGroupAddress, configuration.NewsGroupUseSsl);
                    client.AuthenticateUser(configuration.NewsGroupUsername, configuration.NewsGroupPassword);

                    client.SelectNewsgroup(postInfo.PostedGroups[0]);  //TODO: verify if required.

                    ArticleHeadersDictionary headers = new ArticleHeadersDictionary();
                    headers.AddHeader("From", configuration.FromAddress);
                    headers.AddHeader("Subject", subject);
                    foreach (var newsGroup in postInfo.PostedGroups)
                    {
                        headers.AddHeader("Newsgroups", newsGroup);
                    }
                    headers.AddHeader("Date", postInfo.PostedDateTime.ToString());

                    String partMessageId = 
                        client.PostArticle(new ArticleHeadersDictionaryEnumerator(headers), prefix, yEncPart.EncodedLines, suffix);
                    lock (postInfo.Segments)
                    {
                        postInfo.Segments.Add(new PostedFileSegment
                        {
                            MessageId = partMessageId,
                            Bytes = yEncPart.Size,
                            SegmentNumber = yEncPart.Number
                        });
                    }
                }
                OnFilePartPosted(yEncPart);
            }
            catch(Exception ex)
            {
                throw;
            }
        }
    }
}
