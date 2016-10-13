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
using Util.Configuration;

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
                var configuration = Settings.LoadSettings();

                Watcher watcher = new Watcher(configuration);
                watcher.Start();
                log.Info("FileSystemWatcher started");
                Console.WriteLine("FileSystemWatcher started");

                AutoPoster poster = new AutoPoster(configuration);
                poster.Start();
                log.Info("Autoposter started");
                Console.WriteLine("Autoposter started");

                IndexerNotifierBase notifier = IndexerNotifierBase.GetActiveNotifier(configuration);
                if (notifier != null)
                {
                    notifier.Start();
                    log.Info("Notifier started");
                    Console.WriteLine("Notifier started");
                }
                else
                {
                    log.Info("No notifier");
                    Console.WriteLine("No notifier");
                }

                IndexerVerifierBase verifier = IndexerVerifierBase.GetActiveVerifier(configuration);
                if (verifier != null)
                {
                    verifier.Start();
                    log.Info("Verifier started");
                    Console.WriteLine("Verifier started");
                }
                else
                {
                    log.Info("No verifier");
                    Console.WriteLine("No verifier");
                }

                DatabaseCleaner cleaner = new DatabaseCleaner(configuration);
                cleaner.Start();
                log.Info("DB Cleaner started");
                Console.WriteLine("DB Cleaner started");

                Console.WriteLine("Press the \"s\" key to stop after the current operations have finished.");

                Boolean stop = false;
                while (!stop)
                {
                    var keyInfo = Console.ReadKey();
                    stop = keyInfo.KeyChar == 's' || keyInfo.KeyChar == 'S';
                }

                cleaner.Stop();
                log.Info("DB Cleaner stopped");
                Console.WriteLine("DB Cleaner stopped");

                watcher.Stop();
                log.Info("FileSystemWatcher stopped");
                Console.WriteLine("FileSystemWatcher stopped");

                if (verifier != null)
                {
                    verifier.Stop();
                    log.Info("Verifier stopped");
                    Console.WriteLine("Verifier stopped");
                }

                if (notifier != null)
                {
                    notifier.Stop();
                    log.Info("Notifier stopped");
                    Console.WriteLine("Notifier stopped");
                }

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
