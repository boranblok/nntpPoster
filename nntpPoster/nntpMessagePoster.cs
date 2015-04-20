using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NntpClientLib;

namespace nntpPoster
{
    public class nntpMessagePoster : InntpMessagePoster
    {
        private List<Task> RunningTasks = new List<Task>();

        public void PostMessage(String subject, List<String> prefix, Byte[] yEncBody, List<String> suffix)
        {
            Boolean waitForFreeThread = true;
            while(waitForFreeThread)
            {
                lock (RunningTasks)
                {
                    if (RunningTasks.Count < PostSettings.MaxConnectionCount)
                    {
                        Task task = new Task(() => PostMessageTask(subject, prefix, yEncBody, suffix));
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

        private void PostMessageTask(String subject, List<String> prefix, Byte[] yEncBody, List<String> suffix)
        {
            try
            {
                using (Rfc977NntpClientWithExtensions client = new Rfc977NntpClientWithExtensions())
                {
                    client.Connect(PostSettings.NewsGroupAddress, PostSettings.NewsGroupUseSsl);
                    client.AuthenticateUser(PostSettings.NewsGroupUsername, PostSettings.NewsGroupPassword);

                    string newsgroup = PostSettings.TargetNewsgroup;
                    client.SelectNewsgroup(newsgroup);

                    ArticleHeadersDictionary headers = new ArticleHeadersDictionary();
                    headers.AddHeader("From", PostSettings.FromAddress);
                    headers.AddHeader("Subject", subject);
                    headers.AddHeader("Newsgroups", newsgroup);
                    headers.AddHeader("Date", new NntpDateTime(DateTime.Now).ToString());

                    client.PostArticle(new ArticleHeadersDictionaryEnumerator(headers), prefix, yEncBody, suffix);
                }
            }
            catch(Exception ex)
            {
                throw;
            }
        }
    }
}
