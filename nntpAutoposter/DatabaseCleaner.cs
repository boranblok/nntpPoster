using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Util.Configuration;

namespace nntpAutoposter
{
    public class DatabaseCleaner
    {
        protected static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Object monitor = new Object();
        private Settings configuration;
        private Task MyTask;
        private Boolean StopRequested;

        public DatabaseCleaner(Settings configuration)
        {
            this.configuration = configuration;
            StopRequested = false;
            MyTask = new Task(CleanupTask, TaskCreationOptions.LongRunning);
        }

        private void CleanupTask()
        {
            while (!StopRequested)
            {
                try
                {
                    log.InfoFormat("Cleaning up database, removing entries older than {0} days", configuration.DatabaseCleanupKeepdays);
                    DBHandler.Instance.CleanUploadEntries(configuration.DatabaseCleanupKeepdays);
                }
                catch (Exception ex)
                {
                    log.Fatal("Fatal exception in the database cleanup task.", ex);
                    Environment.Exit(1);
                }
                lock (monitor)
                {
                    if (StopRequested)
                    {
                        break;
                    }
                    Monitor.Wait(monitor, configuration.DatabaseCleanupHours * 1000 * 60 * 60);
                }
            }
        }

        public void Start()
        {
            MyTask.Start();
        }

        public void Stop(Int32 millisecondsTimeout = Timeout.Infinite)
        {
            lock (monitor)
            {
                StopRequested = true;
                Monitor.Pulse(monitor);
            }
            MyTask.Wait(millisecondsTimeout);
        }
    }
}
