using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using nntpPoster;
using Util;

namespace nntpAutoposter
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            try
            {
                var configuration = new AutoPosterConfig();

                Watcher watcher = new Watcher(configuration);
                watcher.Start();
                log.Info("FileSystemWatcher started");
                Console.WriteLine("FileSystemWatcher started");

                AutoPoster poster = new AutoPoster(configuration);
                poster.Start();
                log.Info("Autoposter started");
                Console.WriteLine("Autoposter started");

                IndexerNotifier notifier = new IndexerNotifier(configuration);
                notifier.Start();
                log.Info("Notifier started");
                Console.WriteLine("Notifier started");

                IndexerVerifier verifier = new IndexerVerifier(configuration);
                verifier.Start();
                log.Info("Verifier started");
                Console.WriteLine("Verifier started");

                Console.WriteLine("Press the \"s\" key to stop after the current operations have finished.");

                Boolean stop = false;
                while (!stop)
                {
                    var keyInfo = Console.ReadKey();
                    stop = keyInfo.KeyChar == 's' || keyInfo.KeyChar == 'S';
                }

                watcher.Stop();
                log.Info("FileSystemWatcher stopped");
                Console.WriteLine("FileSystemWatcher stopped");

                verifier.Stop();
                log.Info("Verifier stopped");
                Console.WriteLine("Verifier stopped");

                notifier.Stop();
                log.Info("Notifier stopped");
                Console.WriteLine("Notifier stopped");

                poster.Stop();
                log.Info("Autoposter stopped");
                Console.WriteLine("Autoposter stopped");
            }
            catch(Exception ex)
            {
                log.Fatal("Fatal exception when starting the autoposter.", ex);
                throw;
            }
        }
    }
}
