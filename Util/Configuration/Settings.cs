using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;

namespace Util.Configuration
{
    public class Settings
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Settings()
        {
            RarNParSettings = new List<RarNParSetting>();
            WatchFolderSettings = new List<WatchFolderSettings>();
        }

        public static Settings LoadSettings()
        {
            return SettingsLoader.LoadSettings();
        }

        public WatchFolderSettings GetWatchFolderSettings(String shortName)
        {
            if(WatchFolderSettings.Any(s => s.ShortName == shortName))
                return WatchFolderSettings.First(s => s.ShortName == shortName);
            return WatchFolderSettings.First();
        }

        public static Int32 DetermineOptimalRarSize(Int32 configuredRarSize, Int32 yEncLineSize, Int32 yEncLinesPerMessage)
        {
            var configuredBytes = configuredRarSize * 1024 * 1024;
            var blockSizeBytes = yEncLineSize * yEncLinesPerMessage;
            var optimalNumberOfBlocks = (Int32)Math.Round((Decimal)configuredBytes / blockSizeBytes, 0, MidpointRounding.AwayFromZero);
            return optimalNumberOfBlocks * blockSizeBytes;
        }

        public Int32 YEncPartSize
        {
            get
            {
                return YEncLineSize * YEncLinesPerMessage;
            }
        }

        //Settings the user must change

        public String NewsGroupAddress { get; set; }
        public Int32 NewsGroupPort { get; set; }
        public String NewsGroupUsername { get; set; }
        public String NewsGroupPassword { get; set; }
        public Boolean NewsGroupUseSsl { get; set; }
        public String ObfuscatedNotificationUrl { get; set; }
        public String SearchUrl { get; set; }
        public String IndexerRenameMapSource { get; set; }
        public String IndexerRenameMapTarget { get; set; }
        public List<WatchFolderSettings> WatchFolderSettings { get; set; }

                
        //Settings that the user might want to change

        public Int32 MaxConnectionCount { get; set; }
        public DirectoryInfo WorkingFolder { get; set; }
        public DirectoryInfo NzbOutputFolder { get; set; }        
        public DirectoryInfo BackupFolder { get; set; }
        public Int32 MaxRepostCount { get; set; }
        public DirectoryInfo PostFailedFolder { get; set; }
        public String RarLocation { get; set; }
        public String RarExtraParameters { get; set; }
        public String ParLocation { get; set; }
        public String ParExtraParameters { get; set; }
        public String MkvPropEditLocation { get; set; }
        public String FFmpegLocation { get; set; }
        public String NotificationType { get; set; }
        public String VerificationType { get; set; }


        // Settings the user probably shouldnt change.

        public Boolean RemoveAfterVerify { get; set; }
        public Int32 FilesystemCheckIntervalSeconds { get; set; }
        public Int32 FilesystemCheckTesholdMinutes { get; set; }
        public Int32 AutoposterIntervalSeconds { get; set; }
        public Int32 NotifierIntervalMinutes { get; set; }
        public Int32 VerifierIntervalMinutes { get; set; }
        public Int32 DatabaseCleanupHours { get; set; }
        public Int32 DatabaseCleanupKeepdays { get; set; }
        public Int32 VerifySimilarityPercentageTreshold { get; set; }
        public Int32 VerifyAfterMinutes { get; set; }
        public Int32 RepostAfterMinutes { get; set; }
        public Int32 MaxRetryCount { get; set; }
        public Int32 RetryDelaySeconds { get; set; }
        public Int32 InactiveProcessTimeout { get; set; }
        public Int32 YEncLineSize { get; set; }
        public Int32 YEncLinesPerMessage { get; set; }
        public List<RarNParSetting> RarNParSettings { get; set; }
    }
}
