using System;

namespace nntpAutoposter
{
    class Program
    {
        static AutoPosterConfig _configuration;
        static void Main()
        {
            _configuration = new AutoPosterConfig();

            var watcher = new Watcher(_configuration);
            watcher.Start();
            Console.WriteLine("FileSystemWatcher started");

            var poster = new AutoPoster(_configuration);
            poster.Start();
            Console.WriteLine("Autoposter started");

            var notifier = new IndexerNotifier(_configuration);
            notifier.Start();
            Console.WriteLine("Notifier started");

            var verifier = new IndexerVerifier(_configuration);
            verifier.Start();
            Console.WriteLine("Verifier started");


            Console.WriteLine("Press any key to stop after the current operations have finished.");
            Console.ReadKey();


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
