using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using log4net;

namespace Util.Configuration
{
    [DataContract(Namespace = "Util.Configuration")]
    public class Settings
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly DataContractSerializer Serializer = new DataContractSerializer(typeof(Settings));
        private static readonly XmlWriterSettings WriterSettings = new XmlWriterSettings { Indent = true };

        public Settings()
        {
            RarNParSettings = new List<RarNParSetting>();
            WatchFolderSettings = new List<WatchFolderSettings>();
        }

        public static Settings LoadSettings(String fileName = "settings.config")
        {
            var configFile = new FileInfo(fileName);
            if(!configFile.Exists)
                throw new Exception("Cannot file configuration file with name: " + fileName);

            var settings = Serializer.ReadObject(configFile.OpenRead()) as Settings;
            settings.ValidateSettings();
            return settings;
        }

        public void SaveSettings(String fileName = "settings.config")
        {
            using (var writer = XmlWriter.Create(fileName, WriterSettings))
            {
                Serializer.WriteStartObject(writer, this);
                writer.WriteAttributeString("xmlns", "a", null, "http://schemas.microsoft.com/2003/10/Serialization/Arrays");
                Serializer.WriteObjectContent(writer, this);
                Serializer.WriteEndObject(writer);
            }
        }

        public WatchFolderSettings GetWatchFolderSettings(String shortName)
        {
            if(WatchFolderSettings.Any(s => s.ShortName == shortName))
                return WatchFolderSettings.First(s => s.ShortName == shortName);
            return WatchFolderSettings.First();
        }

        private void ValidateSettings()
        {
            if(WatchFolderSettings.GroupBy(s => s.ShortName).Any(g => g.Count() > 1))
                throw  new Exception("The watchfolder short name has to be unique.");

            if (WatchFolderSettings.GroupBy(s => s.PathString).Any(g => g.Count() > 1))
                throw new Exception("The watchfolder path has to be unique.");

            if (IndexerRenameMapSource != null && IndexerRenameMapTarget != null)
            {
                if(IndexerRenameMapSource.Length != IndexerRenameMapTarget.Length)
                    throw new Exception("IndexerRenameMapSource and IndexerRenameMap target need to be of same length.");
            }

            if (IndexerRenameMapSource == null)
            {
                if (IndexerRenameMapTarget != null)
                    throw new Exception("IndexerRenameMapSource and IndexerRenameMap target need to be of same length.");
            }

            if (IndexerRenameMapTarget == null)
            {
                if(IndexerRenameMapSource != null)
                    throw new Exception("IndexerRenameMapSource and IndexerRenameMap target need to be of same length.");
            }

            if(WatchFolderSettings.Any(s => s.ApplyRandomPassword) && String.IsNullOrWhiteSpace(NzbOutputFolderString))
            {
                log.Warn("ApplyRandomPassword is set to true for a watchfolder but NZB output folder is not set.");
                log.Warn("You will have to check the SQLite3 database to know what password was used for a release.");
            }
        }

        internal static DirectoryInfo GetOrCreateFolder(String workingFolderString)
        {
            if (String.IsNullOrWhiteSpace(workingFolderString))
                return null;

            var folder = new DirectoryInfo(workingFolderString);
            if (!folder.Exists)
            {
                folder.Create();
                folder.Refresh();
            }

            return folder;
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

        [DataMember(Order = 0)]
        public String NewsGroupAddress { get; set; }

        [DataMember(Order = 1)]
        public Int32 NewsGroupPort { get; set; }

        [DataMember(Order = 2)]
        public String NewsGroupUsername { get; set; }

        [DataMember(Order = 3)]
        public String NewsGroupPassword { get; set; }

        [DataMember(Order = 4)]
        public Boolean NewsGroupUseSsl { get; set; }

        [DataMember(Order = 5)]
        public String ObfuscatedNotificationUrl { get; set; }

        [DataMember(Order = 6)]
        public String SearchUrl { get; set; }

        [DataMember(Order = 7)]
        public String IndexerRenameMapSource { get; set; }

        [DataMember(Order = 8)]
        public String IndexerRenameMapTarget { get; set; }

        [DataMember(IsRequired = true, Order = 9)]
        public List<WatchFolderSettings> WatchFolderSettings { get; set; }



        //Settings that the user might want to change

        [DataMember(Order = 10)]
        public Int32 MaxConnectionCount { get; set; }

        
        [DataMember(Order = 11, Name = "WorkingFolder")]
        public String WorkingFolderString { get; set; }
        public DirectoryInfo WorkingFolder
        {
            get { return GetOrCreateFolder(WorkingFolderString); }
            set { WorkingFolderString = value.FullName; }
        }

        [DataMember(Order = 12, Name = "NzbOutputFolder")]
        public String NzbOutputFolderString { get; set; }
        public DirectoryInfo NzbOutputFolder
        {
            get { return GetOrCreateFolder(NzbOutputFolderString); }
            set { NzbOutputFolderString = value.FullName; }
        }

        [DataMember(Order = 13, Name = "BackupFolder")]
        public String BackupFolderString { get; set; }
        public DirectoryInfo BackupFolder
        {
            get { return GetOrCreateFolder(BackupFolderString); }
            set { BackupFolderString = value.FullName; }
        }

        [DataMember(Order = 14)]
        public Int32 MaxRepostCount { get; set; }

        [DataMember(Order = 15, Name = "PostFailedFolder")]
        public String PostFailedFolderString { get; set; }
        public DirectoryInfo PostFailedFolder
        {
            get { return GetOrCreateFolder(PostFailedFolderString); }
            set { PostFailedFolderString = value.FullName; }
        }

        [DataMember(Order = 16)]
        public String RarLocation { get; set; }

        [DataMember(Order = 17)]
        public String RarExtraParameters { get; set; }

        [DataMember(Order = 18)]
        public String ParLocation { get; set; }

        [DataMember(Order = 19)]
        public String ParExtraParameters { get; set; }

        [DataMember(Order = 20)]
        public String MkvPropEditLocation { get; set; }

        [DataMember(Order = 21)]
        public String FFmpegLocation { get; set; }


        // Settings the user probably shouldnt change.


        [DataMember(Order = 22)]
        public Boolean RemoveAfterVerify { get; set; }

        [DataMember(Order = 23)]
        public Int32 FilesystemCheckIntervalMillis { get; set; }

        [DataMember(Order = 24)]
        public Int32 FilesystemCheckTesholdMinutes { get; set; }

        [DataMember(Order = 25)]
        public Int32 AutoposterIntervalMillis { get; set; }

        [DataMember(Order = 26)]
        public Int32 NotifierIntervalMinutes { get; set; }

        [DataMember(Order = 27)]
        public Int32 VerifierIntervalMinutes { get; set; }

        [DataMember(Order = 28)]
        public Int32 VerifySimilarityPercentageTreshold { get; set; }

        [DataMember(Order = 29)]
        public Int32 RepostAfterMinutes { get; set; }

        [DataMember(Order = 30)]
        public Int32 MaxRetryCount { get; set; }

        [DataMember(Order = 31)]
        public Int32 InactiveProcessTimeout { get; set; }

        [DataMember(Order = 32)]
        public Int32 YEncLineSize { get; set; }

        [DataMember(Order = 33)]
        public Int32 YEncLinesPerMessage { get; set; }

        [DataMember(Order = 34)]
        public String DatabaseFile { get; set; }

        [DataMember(IsRequired = true, Order = 35)]
        public List<RarNParSetting> RarNParSettings { get; set; }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            MaxRepostCount = 3;
            PostFailedFolderString = "uploadfailed";
        }
    }
}
