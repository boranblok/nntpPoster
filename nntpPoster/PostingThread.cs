using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PostingNntpClient;

namespace nntpPoster
{
    class PostingThread : IDisposable
    {
        private Object monitor = new Object();
        private Boolean StopRequested;
        private Boolean Finished;
        private Task MyTask;

        private SimpleNntpPostingClient _client;
        private Boolean _continuePosting;

        private UsenetPosterConfig _configuration;
        private NewsHostConnectionInfo _connectionInfo;        
        private Queue<nntpMessage> _messageQueue;
        

        public PostingThread(UsenetPosterConfig configuration, NewsHostConnectionInfo connectionInfo, 
            Queue<nntpMessage> messageQueue)
        {
            _configuration = configuration;
            _connectionInfo = connectionInfo;
            _messageQueue = messageQueue;
            MyTask = new Task(PostingTask, TaskCreationOptions.LongRunning);
        }

        private void Start()
        {
            StopRequested = false;
            MyTask.Start();
        }

        private void Stop()
        {
            lock (monitor)
            {
                StopRequested = true;
                Monitor.Pulse(monitor);
            }
            MyTask.Wait();
        }

        private void PostingTask()
        {
            while (!Finished)
            {
                var message = GetNextMessageToPost();
                if(message != null)
                {
                    if (_client == null)
                    {
                        _client = new SimpleNntpPostingClient(connectionInfo);
                        _client.Connect();
                    }

                    _client.PostYEncMessage(
                        _configuration.FromAddress,
                        message.Subject,
                        message.PostInfo.PostedGroups,
                        message.PostInfo.PostedDateTime,
                        message.Prefix,
                        message.YEncFilePart.EncodedLines,
                        message.Suffix);
                }
                else
                {
                    if (_client != null)         //If the queue runs dry we close the connection
                    {
                        _client.Dispose();
                        _client = null;
                    }
                    if (StopRequested)
                    {
                        Finished = true;
                    }
                    else
                    {
                        lock (monitor)
                        {
                            if(Finished)
                            {
                                break;
                            }
                            if (StopRequested)
                            {
                                Finished = true;
                                break;
                            }
                            Monitor.Wait(monitor, 100);
                        }
                    }
                }
            }
        }

        private nntpMessage GetNextMessageToPost()
        {
            lock (_messageQueue)
            {
                if (_messageQueue.Count > 0)
                    return _messageQueue.Dequeue();
            }
            if (!StopRequested) //If stop is requested it is logical the queue gets empty.
                Console.WriteLine("Warning, posting thread is starved, reduce threads to make more optimal use of resources.");
            return null;
        }

        public void Dispose()
        {
            lock (monitor)
            {
                StopRequested = true;
                Finished = true;
                Monitor.Pulse(monitor);
            }
            MyTask.Wait();
            if (_client != null)
                _client.Dispose();
        }
    }
}
