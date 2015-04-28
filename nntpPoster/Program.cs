using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NntpClientLib;

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
            FileInfo file = new FileInfo(args[0]);
            if (!file.Exists)
            {
                Console.WriteLine("The supplied file does not exist.");
                return 2;
            }
            UsenetPosterConfig config = new UsenetPosterConfig();
            UsenetPoster poster = new UsenetPoster(config);
            poster.newUploadSpeedReport += poster_newUploadSpeedReport;
            poster.PostToUsenet(file);

#if DEBUG       //VS does not halt after execution in debug mode.
            Console.WriteLine("Finished");
            Console.ReadKey();
#endif

            return 0;
        }

        static void poster_newUploadSpeedReport(object sender, UploadSpeedReport e)
        {
            Console.Write("\r" + e.ToString() + "          ");
        }
    }
}
