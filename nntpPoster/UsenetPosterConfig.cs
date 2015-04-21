using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nntpPoster
{
    //TODO: this entire thing could be more context dependent instead of from appconfig.
    public class UsenetPosterConfig
    {
        public String FromAddress { get; set; }
        public String NewsGroupAddress { get; set; }
        public String NewsGroupUsername { get; set; }
        public String NewsGroupPassword { get; set; }
        public Boolean NewsGroupUseSsl { get; set; }
        public String[] TargetNewsgroups { get; set; }  //TODO: make multiple value

        public Int32 MaxConnectionCount { get; set; }

        public Int32 YEncLineSize { get; set; }
        public Int32 YEncLinesPerMessage { get; set; }

        public UsenetPosterConfig()
        {
            FromAddress = ConfigurationManager.AppSettings["FromAddress"];

            NewsGroupAddress = ConfigurationManager.AppSettings["NewsGroupAddress"];
            NewsGroupUsername = ConfigurationManager.AppSettings["NewsGroupUsername"];
            NewsGroupPassword = ConfigurationManager.AppSettings["NewsGroupPassword"];
            NewsGroupUseSsl = Boolean.Parse(ConfigurationManager.AppSettings["NewsGroupUseSsl"]);
            TargetNewsgroups = ConfigurationManager.AppSettings["TargetNewsgroup"]
                .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            MaxConnectionCount = Int32.Parse(ConfigurationManager.AppSettings["MaxConnectionCount"]);

            YEncLineSize = Int32.Parse(ConfigurationManager.AppSettings["yEncLineSize"]);
            YEncLinesPerMessage = Int32.Parse(ConfigurationManager.AppSettings["yEncLinesPerMessage"]);
        }
    }
}
