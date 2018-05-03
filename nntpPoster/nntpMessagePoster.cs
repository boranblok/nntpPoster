using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using nntpPoster.yEncLib;
using PostingNntpClient;
using Util.Configuration;

namespace nntpPoster
{
    public class NntpMessagePoster : InntpMessagePoster, IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Object monitor = new Object();
        private Settings configuration;
        private WatchFolderSettings folderConfiguration;
        private NewsHostConnectionInfo connectionInfo;

        private Queue<NntpMessage> MessagesToPost;
        private List<PostingThread> PostingThreads;
        private Boolean IsPosting;

        public NntpMessagePoster(Settings configuration, WatchFolderSettings folderConfiguration)
        {
            this.configuration = configuration;
            this.folderConfiguration = folderConfiguration;
            connectionInfo = new NewsHostConnectionInfo()
            {
                Address = configuration.NewsGroupAddress,
                Port = configuration.NewsGroupPort,
                UseSsl = configuration.NewsGroupUseSsl,
                Username = configuration.NewsGroupUsername,
                Password = configuration.NewsGroupPassword,
                TcpTimeoutSeconds = configuration.NntpConnectionTimeoutSeconds                
            };

            MessagesToPost = new Queue<NntpMessage>();
            PostingThreads = ConstructPostingThreads();
            IsPosting = false;
        }

        private List<PostingThread> ConstructPostingThreads()
        {
            List<PostingThread> postingThreads = new List<PostingThread>();
            for (int i = 0; i < configuration.MaxConnectionCount; i++)
            {
                var postingThread = new PostingThread(configuration, folderConfiguration, connectionInfo, MessagesToPost);
                postingThread.MessagePosted += PostingThread_MessagePosted;
                postingThreads.Add(postingThread);
            }

            return postingThreads;
        }

        void PostingThread_MessagePosted(object sender, NntpMessage e)  //TODO propagate this further instead of yEnc part? (combine the two ?)
        {
            OnFilePartPosted(e.YEncFilePart);
        }

        private void StartPostingThreadsIfNotStarted()
        {
            if (!IsPosting)
            {
                log.Info("we were not posting yet, starting posting threads.");
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
            PartPosted?.Invoke(this, e);
        }

        public void PostMessage(NntpMessage message)
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
            log.Info("Verifying if we already started posting.");
            //If the amount of blocks to post was not enough to trigger the start of posting, we trigger it here.
            StartPostingThreadsIfNotStarted();

            log.Info("Waiting for the final messages to be posted.");
            Task.WaitAll(PostingThreads.Select(t => t.RequestStop()).ToArray());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                log.Debug("Disposing Posting threads.");
                PostingThreads.ForEach(t => t.Dispose());
            }
        }
    }
}
