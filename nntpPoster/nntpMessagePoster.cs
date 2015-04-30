using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using nntpPoster.yEncLib;
using PostingNntpClient;

namespace nntpPoster
{
    public class nntpMessagePoster : InntpMessagePoster
    {
        private UsenetPosterConfig configuration;
        private NewsHostConnectionInfo connectionInfo;
        public nntpMessagePoster(UsenetPosterConfig configuration)
        {
            this.configuration = configuration;
            connectionInfo = new NewsHostConnectionInfo()
            {
                Address = configuration.NewsGroupAddress,
                Port = 443,
                UseSsl = configuration.NewsGroupUseSsl,
                Username = configuration.NewsGroupUsername,
                Password = configuration.NewsGroupPassword
            };
        }
        private List<Task> RunningTasks = new List<Task>();
        public event EventHandler<YEncFilePart> PartPosted;

        protected virtual void OnFilePartPosted(YEncFilePart e)
        {
            if (PartPosted != null) PartPosted(this, e);
        }

        public void PostMessage(String subject, List<String> prefix, YEncFilePart yEncPart, List<String> suffix, PostedFileInfo postInfo)
        {
            Boolean waitForFreeThread = true;
            while (waitForFreeThread)
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
                if (waitForFreeThread)
                    Thread.Sleep(10);     //TODO: optimize sleep value here.
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
            var retryCount = 0;
            var retry = true;
            while (retry && retryCount < configuration.MaxRetryCount)
            {
                try
                {
                    using (SimpleNntpPostingClient client = new SimpleNntpPostingClient(connectionInfo))
                    {
                        client.Connect();

                        var partMessageId = client.PostYEncMessage(
                                configuration.FromAddress,
                                subject,
                                postInfo.PostedGroups,
                                postInfo.PostedDateTime,
                                prefix,
                                yEncPart.EncodedLines,
                                suffix
                            );
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
                    retry = false;
                    OnFilePartPosted(yEncPart);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Posting yEnc message failed:");
                    Console.WriteLine(ex.ToString());
                    if (retryCount++ < configuration.MaxRetryCount)
                        Console.WriteLine("Retrying to post message, attempt {0}", retryCount);
                    else
                        Console.WriteLine("Maximum retry attempts reached. Posting is probably corrupt.", retryCount);
                }
            }
        }
    }
}
