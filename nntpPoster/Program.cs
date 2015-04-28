using System;
using System.IO;

namespace nntpPoster
{
    class Program
    {    
        static Int32 Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Please supply a filename to upload.");
                return 1;
            }
            var file = new FileInfo(args[0]);
            if (!file.Exists)
            {
                Console.WriteLine("The supplied file does not exist.");
                return 2;
            }
            var config = new UsenetPosterConfig();
            var poster = new UsenetPoster(config);
            poster.NewUploadSpeedReport += poster_newUploadSpeedReport;
            poster.PostToUsenet(file);

#if DEBUG       //VS does not halt after execution in debug mode.
            Console.WriteLine("Finished");
            Console.ReadKey();
#endif

            return 0;
        }

        static void poster_newUploadSpeedReport(object sender, UploadSpeedReport e)
        {
            Console.Write("\r" + e + "          ");
        }
    }
}
