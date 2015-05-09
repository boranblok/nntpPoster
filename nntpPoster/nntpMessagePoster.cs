using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using nntpPoster.yEncLib;
using PostingNntpClient;

namespace nntpPoster
{
    public class nntpMessagePoster : InntpMessagePoster, IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Object monitor = new Object();
        private UsenetPosterConfig configuration;
        private NewsHostConnectionInfo connectionInfo;

        private Queue<nntpMessage> MessagesToPost;
        private List<PostingThread> PostingThreads;
        private Boolean IsPosting;

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

            MessagesToPost = new Queue<nntpMessage>();
            PostingThreads = ConstructPostingThreads();
            IsPosting = false;
        }

        private List<PostingThread> ConstructPostingThreads()
        {
            List<PostingThread> postingThreads = new List<PostingThread>();
            for (int i = 0; i < configuration.MaxConnectionCount; i++)
            {
                var postingThread = new PostingThread(configuration, connectionInfo, MessagesToPost);
                postingThread.MessagePosted += postingThread_MessagePosted;
                postingThreads.Add(postingThread);
            }

            return postingThreads;
        }

        void postingThread_MessagePosted(object sender, nntpMessage e)  //TODO propagate this further instead of yEnc part? (combine the two ?)
        {
            OnFilePartPosted(e.YEncFilePart);
        }

        private void StartPostingThreadsIfNotStarted()
        {
            if (!IsPosting)
            {
                IsPosting = true;
                PostingThreads.ForEach(t => t.Start());
            }
        }

        public event EventHandler<YEncFilePart> PartPosted;
        protected virtual void OnFilePartPosted(YEncFilePart e)
        {
            lock (monitor)
            {
                Monitor.Pulse(monitor);
            }
            if (PartPosted != null) PartPosted(this, e);
        }

        public void PostMessage(nntpMessage message)
        {
            Boolean wait = true;
            while (wait)
            {
                lock (MessagesToPost)
                {
                    if (MessagesToPost.Count <= configuration.MaxConnectionCount * 2)
                    {
                        if (MessagesToPost.Count >= configuration.MaxConnectionCount)
                            StartPostingThreadsIfNotStarted();

                        MessagesToPost.Enqueue(message);
                        wait = false;
                    }
                }
                if (wait)
                {
                    lock (monitor)
                    {
                        if (!wait)
                        {
                            break;
                        }
                        Monitor.Wait(monitor, 1000);
                    }
                }
            }
        }

        public void WaitTillCompletion()
        {
            //If the amount of blocks to post was not enough to trigger the start of posting, we trigger it here.
            StartPostingThreadsIfNotStarted();

            Task.WaitAll(PostingThreads.Select(t => t.RequestStop()).ToArray());
        }

        public void Dispose()
        {
            log.Debug("Disposing Posting threads.");
            PostingThreads.ForEach(t => t.Dispose());
        }
    }
}
