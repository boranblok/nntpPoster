using System;
using System.Configuration;
using System.IO;

namespace nntpAutoposter
{
    //TODO: this entire thing could be more context dependent instead of from appconfig.
    public class AutoPosterConfig
    {
        public String PostTag { get; set; }

        public DirectoryInfo WatchFolder { get; set; }        
        public DirectoryInfo BackupFolder { get; set; }
        public Boolean RemoveAfterVerify { get; set; }

        public Int32 AutoposterIntervalMillis { get; set; }
        public Int32 NotifierIntervalMinutes { get; set; }
        public Int32 VerifierIntervalMinutes { get; set; }

        public Boolean UseObscufation { get; set; }
        public String ObscufatedNotificationUrl { get; set; }
        public String SearchUrl { get; set; }
        public Int32 VerifySimilarityPercentageTreshold { get; set; }
        public Int32 MinRepostAgeMinutes { get; set; }
        public Int32 MaxRepostAgeMinutes { get; set; }

        public Boolean StripFileMetadata { get; set; }
        public String MkvPropEditLocation { get; set; }
        public String FFmpegLocation { get; set; }

        public AutoPosterConfig()
        {
            PostTag = ConfigurationManager.AppSettings["PostTag"];

            WatchFolder = new DirectoryInfo(ConfigurationManager.AppSettings["WatchFolder"]);
            if (!WatchFolder.Exists)
                WatchFolder.Create();

            BackupFolder = new DirectoryInfo(ConfigurationManager.AppSettings["BackupFolder"]);
            if (!BackupFolder.Exists)
                BackupFolder.Create();
            RemoveAfterVerify = Boolean.Parse(ConfigurationManager.AppSettings["RemoveAfterVerify"]);

            AutoposterIntervalMillis = Int32.Parse(ConfigurationManager.AppSettings["AutoposterIntervalMillis"]);
            NotifierIntervalMinutes = Int32.Parse(ConfigurationManager.AppSettings["NotifierIntervalMinutes"]);
            VerifierIntervalMinutes = Int32.Parse(ConfigurationManager.AppSettings["VerifierIntervalMinutes"]);

            UseObscufation = Boolean.Parse(ConfigurationManager.AppSettings["UseObscufation"]);
            ObscufatedNotificationUrl = ConfigurationManager.AppSettings["ObscufatedNotificationUrl"];
            SearchUrl = ConfigurationManager.AppSettings["SearchUrl"];
            VerifySimilarityPercentageTreshold = Int32.Parse(ConfigurationManager.AppSettings["VerifySimilarityPercentageTreshold"]);
            MinRepostAgeMinutes = Int32.Parse(ConfigurationManager.AppSettings["MinRepostAgeMinutes"]);
            MaxRepostAgeMinutes = Int32.Parse(ConfigurationManager.AppSettings["MaxRepostAgeMinutes"]);

            StripFileMetadata = Boolean.Parse(ConfigurationManager.AppSettings["StripFileMetadata"]);
            MkvPropEditLocation = ConfigurationManager.AppSettings["MkvPropEditLocation"];
            FFmpegLocation = ConfigurationManager.AppSettings["FFmpegLocation"];
        }
    }
}
