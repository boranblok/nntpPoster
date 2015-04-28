using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NntpClientLib;
using nntpPoster.yEncLib;

namespace nntpPoster
{
    public class NntpMessagePoster : INntpMessagePoster
    {
        private readonly UsenetPosterConfig _configuration;
        public NntpMessagePoster(UsenetPosterConfig configuration)
        {
            _configuration = configuration;
        }
        private readonly List<Task> _runningTasks = new List<Task>();
        public event EventHandler<YEncFilePart> PartPosted;

        protected virtual void OnFilePartPosted(YEncFilePart e)
        {
            if (PartPosted != null) PartPosted(this, e);
        }

        public void PostMessage(String subject, List<String> prefix, YEncFilePart yEncPart, List<String> suffix, PostedFileInfo postInfo)
        {
            var waitForFreeThread = true;
            while (waitForFreeThread)
            {
                lock (_runningTasks)
                {
                    if (_runningTasks.Count < _configuration.MaxConnectionCount)
                    {
                        var task = new Task(() => PostMessageTask(subject, prefix, yEncPart, suffix, postInfo));
                        task.ContinueWith(CleanupTask);
                        _runningTasks.Add(task);
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
            Task.WaitAll(_runningTasks.ToArray());
        }

        private void CleanupTask(Task task)
        {
            lock (_runningTasks)
            {
                _runningTasks.Remove(task);
            }
        }

        private void PostMessageTask(String subject,
            List<String> prefix, YEncFilePart yEncPart, List<String> suffix, PostedFileInfo postInfo)
        {
            using (var client = new Rfc977NntpClientWithExtensions())
            {
                client.Connect(_configuration.NewsGroupAddress, _configuration.NewsGroupUseSsl);
                client.AuthenticateUser(_configuration.NewsGroupUsername, _configuration.NewsGroupPassword);

                var headers = new ArticleHeadersDictionary();
                headers.AddHeader("From", _configuration.FromAddress);
                headers.AddHeader("Subject", subject);
                foreach (var newsGroup in postInfo.PostedGroups)
                {
                    headers.AddHeader("Newsgroups", newsGroup);
                }
                headers.AddHeader("Date", postInfo.PostedDateTime.ToString());

                var partMessageId =
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
    }
}
