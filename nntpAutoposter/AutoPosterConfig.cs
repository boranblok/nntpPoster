using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nntpAutoposter
{
    //TODO: this entire thing could be more context dependent instead of from appconfig.
    public class AutoPosterConfig
    {
        public String PostTag { get; set; }

        public DirectoryInfo WatchFolder { get; set; }        
        public DirectoryInfo BackupFolder { get; set; }
        public Boolean RemoveAfterVerify { get; set; }

        public Int32 FilesystemCheckIntervalMillis { get; set; }
        public Int32 FilesystemCheckTesholdMinutes { get; set; }
        public Int32 AutoposterIntervalMillis { get; set; }
        public Int32 NotifierIntervalMinutes { get; set; }
        public Int32 VerifierIntervalMinutes { get; set; }

        public Boolean UseObfuscation { get; set; }
        public String ObfuscatedNotificationUrl { get; set; }
        public String SearchUrl { get; set; }
        public Int32 VerifySimilarityPercentageTreshold { get; set; }
        public Int32 RepostAfterMinutes { get; set; }

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

            FilesystemCheckIntervalMillis = Int32.Parse(ConfigurationManager.AppSettings["FilesystemCheckIntervalMillis"]);
            FilesystemCheckTesholdMinutes = Int32.Parse(ConfigurationManager.AppSettings["FilesystemCheckTesholdMinutes"]);
            AutoposterIntervalMillis = Int32.Parse(ConfigurationManager.AppSettings["AutoposterIntervalMillis"]);
            NotifierIntervalMinutes = Int32.Parse(ConfigurationManager.AppSettings["NotifierIntervalMinutes"]);
            VerifierIntervalMinutes = Int32.Parse(ConfigurationManager.AppSettings["VerifierIntervalMinutes"]);

            UseObfuscation = Boolean.Parse(ConfigurationManager.AppSettings["UseObfuscation"]);
            ObfuscatedNotificationUrl = ConfigurationManager.AppSettings["ObfuscatedNotificationUrl"];
            SearchUrl = ConfigurationManager.AppSettings["SearchUrl"];
            VerifySimilarityPercentageTreshold = Int32.Parse(ConfigurationManager.AppSettings["VerifySimilarityPercentageTreshold"]);
            RepostAfterMinutes = Int32.Parse(ConfigurationManager.AppSettings["RepostAfterMinutes"]);

            StripFileMetadata = Boolean.Parse(ConfigurationManager.AppSettings["StripFileMetadata"]);
            MkvPropEditLocation = ConfigurationManager.AppSettings["MkvPropEditLocation"];
            FFmpegLocation = ConfigurationManager.AppSettings["FFmpegLocation"];
        }
    }
}
