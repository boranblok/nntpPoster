using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

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
        public String[] TargetNewsgroups { get; set; }

        public Int32 MaxConnectionCount { get; set; }

        public Int32 YEncLineSize { get; set; }
        public Int32 YEncLinesPerMessage { get; set; }

        public DirectoryInfo WorkingFolder { get; set; }
        public String NzbOutputFolder { get; set; }

        public List<RarAndRecoveryRecommendation> RecommendationMap { get; set; }
        public String RarLocation { get; set; }
        public String ParLocation { get; set; }

        public Int32 YEncPartSize 
        { 
            get
            {
                return YEncLineSize * YEncLinesPerMessage;
            }
        }

        public UsenetPosterConfig()
        {
            FromAddress = ConfigurationManager.AppSettings["FromAddress"];

            NewsGroupAddress = ConfigurationManager.AppSettings["NewsGroupAddress"];
            NewsGroupUsername = ConfigurationManager.AppSettings["NewsGroupUsername"];
            NewsGroupPassword = ConfigurationManager.AppSettings["NewsGroupPassword"];
            NewsGroupUseSsl = Boolean.Parse(ConfigurationManager.AppSettings["NewsGroupUseSsl"]);
            TargetNewsgroups = ConfigurationManager.AppSettings["TargetNewsgroups"]
                .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);

            MaxConnectionCount = Int32.Parse(ConfigurationManager.AppSettings["MaxConnectionCount"]);

            YEncLineSize = Int32.Parse(ConfigurationManager.AppSettings["yEncLineSize"]);
            YEncLinesPerMessage = Int32.Parse(ConfigurationManager.AppSettings["yEncLinesPerMessage"]);

            WorkingFolder = new DirectoryInfo(ConfigurationManager.AppSettings["WorkingFolder"]);
            if(!WorkingFolder.Exists)
                WorkingFolder.Create();

            NzbOutputFolder = ConfigurationManager.AppSettings["NzbOutputFolder"];
            if(!String.IsNullOrWhiteSpace(NzbOutputFolder))
            {
                var nzbFolder = new DirectoryInfo(NzbOutputFolder);
                if (!nzbFolder.Exists)
                    nzbFolder.Create();
            }


            RecommendationMap = LoadReccomendationMap(ConfigurationManager.AppSettings["OptimalSizeRarAndPar"]);
            RarLocation = ConfigurationManager.AppSettings["RarLocation"];
            ParLocation = ConfigurationManager.AppSettings["ParLocation"];
        }

        private List<RarAndRecoveryRecommendation> LoadReccomendationMap(String configValue)
        {
            return configValue.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
                .Select(sizeMapping => sizeMapping.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries))
                .Select(configEntry => new RarAndRecoveryRecommendation
            {
                FromFileSize = Int32.Parse(configEntry[0]) * 1024 * 1024, 
                ReccomendedRarSize = DetermineOpticalRarSize(Int32.Parse(configEntry[1])),
                ReccomendedRecoveryPercentage = Int32.Parse(configEntry[2])
            }).ToList();
        }

        private Int32 DetermineOpticalRarSize(Int32 configuredMegabytes)
        {
            var configuredBytes = configuredMegabytes * 1024 * 1024;
            var blockSizeBytes = YEncLineSize * YEncLinesPerMessage;
            var optimalNumberOfBlocks = (Int32)Math.Round((Decimal)configuredBytes / blockSizeBytes, 0, MidpointRounding.AwayFromZero);
            return optimalNumberOfBlocks * blockSizeBytes;
        }
    }
}
