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

        static AutoPosterConfig configuration;
        static void Main(string[] args)
        {
            configuration = new AutoPosterConfig();

            Watcher watcher = new Watcher(configuration);
            watcher.Start();
            Console.WriteLine("FileSystemWatcher started");

            AutoPoster poster = new AutoPoster(configuration);
            poster.Start();
            Console.WriteLine("Autoposter started");

            IndexerNotifier notifier = new IndexerNotifier(configuration);
            notifier.Start();
            Console.WriteLine("Notifier started");

            IndexerVerifier verifier = new IndexerVerifier(configuration);
            verifier.Start();
            Console.WriteLine("Verifier started");

            Console.WriteLine("Press the \"s\" key to stop after the current operations have finished.");

            Boolean stop = false;
            while (!stop)
            {
                var keyInfo = Console.ReadKey();
                stop = keyInfo.KeyChar == 's' || keyInfo.KeyChar == 'S';
            }

            watcher.Stop();
            Console.WriteLine("FileSystemWatcher stopped");

            verifier.Stop();
            Console.WriteLine("Verifier stopped");

            notifier.Stop();
            Console.WriteLine("Notifier stopped");

            poster.Stop();
            Console.WriteLine("Autoposter stopped");
        }
    }
}
