using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace VideoFileRenamer
{
    class Renamer
    {
        //Regular expressions blatantly copied from nzbdrone: https://github.com/Sonarr/Sonarr
        private static readonly Regex TitleRegex = new Regex(@"(?<token>\{(?:\w+)(?<separator>\s|\.|-|_)\w+\})",
                                             RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex EpisodeRegex = new Regex(@"(?<episode>\{episode(?:\:0+)?})",
                                                               RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SeasonRegex = new Regex(@"(?<season>\{season(?:\:0+)?})",
                                                              RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex AbsoluteEpisodeRegex = new Regex(@"(?<absolute>\{absolute(?:\:0+)?})",
                                                               RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex SeasonEpisodePatternRegex = new Regex(@"(?<separator>(?<=}).+?)?(?<seasonEpisode>s?{season(?:\:0+)?}(?<episodeSeparator>e|x)(?<episode>{episode(?:\:0+)?}))(?<separator>.+?(?={))?",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex AbsoluteEpisodePatternRegex = new Regex(@"(?<separator>(?<=}).+?)?(?<absolute>{absolute(?:\:0+)?})(?<separator>.+?(?={))?",
                                                                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex AirDateRegex = new Regex(@"\{Air(\s|\W|_)Date\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex SeriesTitleRegex = new Regex(@"(?<token>\{(?:Series)(?<separator>\s|\.|-|_)Title\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex FilenameCleanupRegex = new Regex(@"\.{2,}", RegexOptions.Compiled);

        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private RenamerConfiguration configuration;


        public Renamer(RenamerConfiguration configuration)
        {
            this.configuration = configuration;
        }       
    }
}
