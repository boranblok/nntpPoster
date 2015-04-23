using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nntpPoster
{
    //TODO: this entire thing could be more context dependent instead of from appconfig.
    public class UsenetPosterConfig
    {
        public String PostTag { get; set; }
        public String FromAddress { get; set; }
        public String NewsGroupAddress { get; set; }
        public String NewsGroupUsername { get; set; }
        public String NewsGroupPassword { get; set; }
        public Boolean NewsGroupUseSsl { get; set; }
        public String[] TargetNewsgroups { get; set; }

        public Int32 MaxConnectionCount { get; set; }

        public Int32 YEncLineSize { get; set; }
        public Int32 YEncLinesPerMessage { get; set; }

        public DirectoryInfo WorkingFolder { get; set; }
        public String NzbOutputFolder { get; set; }

        public List<RarAndRecoveryRecommendation> RecommendationMap { get; set; }
        public String RarToolLocation { get; set; }
        public String ParToolLocation { get; set; }

        public Boolean UseHashing { get; set; }
        public String HashedReleaseNotificationUrl { get; set; }

        public Int32 YEncPartSize 
        { 
            get
            {
                return YEncLineSize * YEncLinesPerMessage;
            }
        }

        public UsenetPosterConfig()
        {
            PostTag = ConfigurationManager.AppSettings["PostTag"];

            FromAddress = ConfigurationManager.AppSettings["FromAddress"];

            NewsGroupAddress = ConfigurationManager.AppSettings["NewsGroupAddress"];
            NewsGroupUsername = ConfigurationManager.AppSettings["NewsGroupUsername"];
            NewsGroupPassword = ConfigurationManager.AppSettings["NewsGroupPassword"];
            NewsGroupUseSsl = Boolean.Parse(ConfigurationManager.AppSettings["NewsGroupUseSsl"]);
            TargetNewsgroups = ConfigurationManager.AppSettings["TargetNewsgroups"]
                .Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);

            MaxConnectionCount = Int32.Parse(ConfigurationManager.AppSettings["MaxConnectionCount"]);

            YEncLineSize = Int32.Parse(ConfigurationManager.AppSettings["yEncLineSize"]);
            YEncLinesPerMessage = Int32.Parse(ConfigurationManager.AppSettings["yEncLinesPerMessage"]);

            WorkingFolder = new DirectoryInfo(ConfigurationManager.AppSettings["WorkingFolder"]);
            if(!WorkingFolder.Exists)
                WorkingFolder.Create();

            NzbOutputFolder = ConfigurationManager.AppSettings["NzbOutputFolder"];
            if(!String.IsNullOrWhiteSpace(NzbOutputFolder))
            {
                DirectoryInfo nzbFolder = new DirectoryInfo(NzbOutputFolder);
                if (!nzbFolder.Exists)
                    nzbFolder.Create();
            }


            RecommendationMap = LoadReccomendationMap(ConfigurationManager.AppSettings["OptimalSizeRarAndPar"]);
            RarToolLocation = ConfigurationManager.AppSettings["RarToolLocation"];
            ParToolLocation = ConfigurationManager.AppSettings["ParToolLocation"];
        }

        private List<RarAndRecoveryRecommendation> LoadReccomendationMap(String configValue)
        {
            return configValue.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries)
                .Select(sizeMapping => sizeMapping.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries))
                .Select(configEntry => new RarAndRecoveryRecommendation
            {
                FromFileSize = Int32.Parse(configEntry[0]) * 1024 * 1024, 
                ReccomendedRarSize = DetermineOpticalRarSize(Int32.Parse(configEntry[1])),
                ReccomendedRecoveryPercentage = Int32.Parse(configEntry[2])
            }).ToList();
        }

        private Int32 DetermineOpticalRarSize(Int32 configuredMegabytes)
        {
            Int32 configuredBytes = configuredMegabytes * 1024 * 1024;
            Int32 blockSizeBytes = YEncLineSize * YEncLinesPerMessage;
            Int32 optimalNumberOfBlocks = (Int32)Math.Round((Decimal)configuredBytes / blockSizeBytes, 0, MidpointRounding.AwayFromZero);
            return optimalNumberOfBlocks * blockSizeBytes;
        }
    }
}
