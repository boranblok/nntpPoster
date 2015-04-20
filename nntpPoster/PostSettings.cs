using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nntpPoster
{
    public class PostSettings
    {
        public static String FromAddress { get; set; }
        public static String NewsGroupAddress { get; set; }
        public static String NewsGroupUsername { get; set; }
        public static String NewsGroupPassword { get; set; }
        public static Boolean NewsGroupUseSsl { get; set; }
        public static String TargetNewsgroup { get; set; }

        public static Int32 MaxConnectionCount { get; set; }

        public static Int32 YEncLineSize { get; set; }
        public static Int32 YEncLinesPerMessage { get; set; }

        static PostSettings()
        {
            FromAddress = ConfigurationManager.AppSettings["FromAddress"];

            NewsGroupAddress = ConfigurationManager.AppSettings["NewsGroupAddress"];
            NewsGroupUsername = ConfigurationManager.AppSettings["NewsGroupUsername"];
            NewsGroupPassword = ConfigurationManager.AppSettings["NewsGroupPassword"];
            NewsGroupUseSsl = Boolean.Parse(ConfigurationManager.AppSettings["NewsGroupUseSsl"]);
            TargetNewsgroup = ConfigurationManager.AppSettings["TargetNewsgroup"];

            MaxConnectionCount = Int32.Parse(ConfigurationManager.AppSettings["MaxConnectionCount"]);

            YEncLineSize = Int32.Parse(ConfigurationManager.AppSettings["yEncLineSize"]);
            YEncLinesPerMessage = Int32.Parse(ConfigurationManager.AppSettings["yEncLinesPerMessage"]);
        }
    }
}
