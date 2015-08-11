using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VideoFileRenamer
{
    class FileTypeDetector
    {
        private static readonly Regex TVShowRegex = new Regex(@"[ \_\.\[\(][s]?\d?\d[x\.e]\d?\d?\d[ \_\.\]\)ex-]",
                                             RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex YearRegex = new Regex(@"[ \_\.\[\(](?:20|19)[0-9]{2}[ \_\.\]\)-]",
                                             RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex AbsoluteEpisodeIndex = new Regex(@"(ep)?[ \_\.\[\(]e?[0-9]{1,3}[ \_\.\]\)-]",
                                             RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex DailyEpisodeRegex = new Regex(@"[ \_\.\[\(](?:20|19)[0-9]{2}[\_\.\-]?(1[0-2]|0?[1-9])[\_\.\-]?(3[01]|[12][0-9]|0?[1-9])[ \_\.\]\)-]",
                                             RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static FileType GetFileType(FileInfo file)
        {
            return FileType.Unknown;
        }
    }
}
