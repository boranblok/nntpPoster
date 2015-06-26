using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoFileRenamer
{
    class RenamerConfiguration
    {
        public DirectoryInfo RootWatchFolder { get; set; }
        public Int32 WatchFolderCheckIntervalSeconds { get; set; }
        public Int32 WatchFolderMinAgeMinutes { get; set; }
        public DirectoryInfo NoMatchFolder { get; set; }
        public String[] TagFolders { get; set; }
        public String[] HandledFileExtensions { get; set; }
        public DirectoryInfo UnhandledFilesFolder { get; set; }
        public DirectoryInfo OutputFolder { get; set; }
        public String FileBotLocation { get; set; }
        public String TVEpisodeFormat { get; set; }
        public String TvDbToUse { get; set; }
        public String MovieFormat { get; set; }
        public String MovieDbToUse { get; set; }
        public String AnimeFormat { get; set; }
        public String AnimeDbToUse { get; set; }

        public RenamerConfiguration()
        {
            RootWatchFolder = new DirectoryInfo(ConfigurationManager.AppSettings["RootWatchFolder"]);
            WatchFolderCheckIntervalSeconds =
                Int32.Parse(ConfigurationManager.AppSettings["WatchFolderCheckIntervalSeconds"]);
            WatchFolderMinAgeMinutes = Int32.Parse(ConfigurationManager.AppSettings["WatchFolderMinAgeMinutes"]);
            NoMatchFolder = new DirectoryInfo(ConfigurationManager.AppSettings["NoMatchFolder"]);
            TagFolders = ConfigurationManager.AppSettings["TagFolders"].Split(',');
            HandledFileExtensions = ConfigurationManager.AppSettings["HandledFileExtensions"].Split(',');
            UnhandledFilesFolder = new DirectoryInfo(ConfigurationManager.AppSettings["UnhandledFilesFolder"]);
            OutputFolder = new DirectoryInfo(ConfigurationManager.AppSettings["OutputFolder"]);
            FileBotLocation = ConfigurationManager.AppSettings["FileBotLocation"];
            TVEpisodeFormat = ConfigurationManager.AppSettings["TVEpisodeFormat"];
            TvDbToUse = ConfigurationManager.AppSettings["TvDbToUse"];
            MovieFormat = ConfigurationManager.AppSettings["MovieFormat"];
            MovieDbToUse = ConfigurationManager.AppSettings["MovieDbToUse"];
            AnimeFormat = ConfigurationManager.AppSettings["AnimeFormat"];
            AnimeDbToUse = ConfigurationManager.AppSettings["AnimeDbToUse"];
        }

        public void InitializeEnvironment()
        {
            if (!RootWatchFolder.Exists)
                RootWatchFolder.Create();

            if (!NoMatchFolder.Exists)
                NoMatchFolder.Create();


            foreach (var tagFolder in TagFolders)
            {
                DirectoryInfo tagFolderInfo = new DirectoryInfo(Path.Combine(RootWatchFolder.FullName, tagFolder));
                if (!tagFolderInfo.Exists)
                    tagFolderInfo.Create();
            }

            if (!UnhandledFilesFolder.Exists)
                UnhandledFilesFolder.Create();

            if (!OutputFolder.Exists)
                OutputFolder.Create();
        }
    }
}
