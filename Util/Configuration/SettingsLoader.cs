using log4net;
using Nini.Config;
using Nini.Ini;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util.Configuration
{
    class SettingsLoader
    {
        private const string WatchfolderSection = "Watchfolder";
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static Settings LoadSettings()
        {
            var settings = GetSettingsFromIniFiles();
            ValidateSettings(settings);
            CreateFolders(settings);
            return settings;
        }

        private static void CreateFolders(Settings settings)
        {
            if (!settings.BackupFolder.Exists)
            {
                settings.BackupFolder.Create();
                settings.BackupFolder.Refresh();
            }
            if(settings.NzbOutputFolder != null && !settings.NzbOutputFolder.Exists)
            {
                settings.NzbOutputFolder.Create();
                settings.NzbOutputFolder.Refresh();
            }
            if(!settings.PostFailedFolder.Exists)
            {
                settings.PostFailedFolder.Create();
                settings.PostFailedFolder.Refresh();
            }
            if(!settings.WorkingFolder.Exists)
            {
                settings.WorkingFolder.Create();
                settings.WorkingFolder.Refresh();
            }
            foreach(var watchfolder in settings.WatchFolderSettings)
            {
                if (!watchfolder.Path.Exists)
                {
                    watchfolder.Path.Create();
                    watchfolder.Path.Refresh();
                }
            }
        }

        private static void ValidateSettings(Settings settings)
        {
            if (settings.WatchFolderSettings.GroupBy(s => s.ShortName).Any(g => g.Count() > 1))
                throw new Exception("The watchfolder short name has to be unique.");

            if (settings.WatchFolderSettings.GroupBy(s => s.Path.FullName).Any(g => g.Count() > 1))
                throw new Exception("The watchfolder path has to be unique.");

            for(int i = 0; i < settings.WatchFolderSettings.Count; i++)
            {
                for(int j = 0; j < settings.WatchFolderSettings.Count; j++)
                {
                    if (i == j)
                        continue;

                    var folder1 = settings.WatchFolderSettings[i].Path.FullName;
                    var folder2 = settings.WatchFolderSettings[j].Path.FullName;

                    if(folder1.IsSubPathOf(folder2))
                        throw new Exception(String.Format("Watchfolder {0} is a subfolder of {1} this would cause loops.", folder1, folder2));
                    if (folder2.IsSubPathOf(folder1))
                        throw new Exception(String.Format("Watchfolder {0} is a subfolder of {1} this would cause loops.", folder2, folder1));
                }
            }

            if (settings.IndexerRenameMapSource != null && settings.IndexerRenameMapTarget != null)
            {
                if (settings.IndexerRenameMapSource.Length != settings.IndexerRenameMapTarget.Length)
                    throw new Exception("IndexerRenameMapSource and IndexerRenameMap target need to be of same length.");
            }

            if (settings.IndexerRenameMapSource == null)
            {
                if (settings.IndexerRenameMapTarget != null)
                    throw new Exception("IndexerRenameMapSource and IndexerRenameMap target need to be of same length.");
            }

            if (settings.IndexerRenameMapTarget == null)
            {
                if (settings.IndexerRenameMapSource != null)
                    throw new Exception("IndexerRenameMapSource and IndexerRenameMap target need to be of same length.");
            }

            if (settings.WatchFolderSettings.Any(s => s.ApplyRandomPassword) && settings.NzbOutputFolder == null)
            {
                log.Warn("ApplyRandomPassword is set to true for a watchfolder but NZB output folder is not set.");
                log.Warn("You will have to check the SQLite3 database to know what password was used for a release.");
            }

            foreach(WatchFolderSettings watchfolderSetting in settings.WatchFolderSettings)
            {
                if(watchfolderSetting.SpreadFilesOverTargetNewsgroups && watchfolderSetting.TargetNewsgroups.Count < 2)
                {
                    log.WarnFormat("Spreading files over newsgroups is enabled but only one newsgroup has been entered for watch folder configuration {0}", watchfolderSetting.ShortName);
                }
            }
        }

        internal static DirectoryInfo GetOrCreateFolder(String folderString)
        {
            if (String.IsNullOrWhiteSpace(folderString))
                return null;

            var folder = new DirectoryInfo(folderString);
            if (!folder.Exists)
            {
                folder.Create();
                folder.Refresh();
            }

            return folder;
        }

        private static Settings GetSettingsFromIniFiles()
        {
            FileInfo defaultConfigFile = new FileInfo("conf/default.ini");
            if (!defaultConfigFile.Exists)
            {
                log.Fatal("The default config file at conf/default.ini does not exist");
                throw new Exception("The default config file at conf/default.ini does not exist");
            }

            IConfigSource baseConfig = new IniConfigSource(new IniDocument("conf/default.ini", IniFileType.MysqlStyle));

            List<String> userConfigFiles = new List<String>(Directory.GetFiles("userconf", "*.ini"));
            userConfigFiles.Sort();
            userConfigFiles.ForEach(f => baseConfig.Merge(new IniConfigSource(new IniDocument(f, IniFileType.MysqlStyle))));
            AddConfigAliasses(baseConfig);

            Settings settings = GetSettingsFromMergedIni(baseConfig);

            LoadFolderConfigs(settings);

            return settings;
        }

        private static Settings GetSettingsFromMergedIni(IConfigSource baseConfig)
        {
            Settings settings = new Settings();

            const string NewsHostSection = "NewsHost";
            settings.NewsGroupAddress = GetSettingString(baseConfig, NewsHostSection, "Address");
            settings.NewsGroupPort = GetSettingInt(baseConfig, NewsHostSection, "Port");
            settings.NewsGroupUsername = GetSettingString(baseConfig, NewsHostSection, "Username");
            settings.NewsGroupPassword = GetSettingString(baseConfig, NewsHostSection, "Password");
            settings.NewsGroupUseSsl = GetSettingBoolean(baseConfig, NewsHostSection, "UseSSL");
            settings.MaxConnectionCount = GetSettingInt(baseConfig, NewsHostSection, "MaxConnectionCount");

            const string IndexerSection = "Indexer";
            settings.NotificationType = GetSettingString(baseConfig, IndexerSection, "NotificationType");
            settings.MaxNotificationAttempts = GetSettingInt(baseConfig, IndexerSection, "MaxNotificationAttempts");
            settings.ObfuscatedNotificationUrl = GetSettingString(baseConfig, IndexerSection, "ObfuscatedNotificationUrl");
            settings.VerificationType = GetSettingString(baseConfig, IndexerSection, "VerificationType");
            settings.SearchUrl = GetSettingString(baseConfig, IndexerSection, "SearchUrl");
            settings.IndexerRenameMapSource = GetSettingString(baseConfig, IndexerSection, "IndexerRenameMapSource");
            settings.IndexerRenameMapTarget = GetSettingString(baseConfig, IndexerSection, "IndexerRenameMapTarget");
            settings.VerifySimilarityPercentageTreshold = GetSettingInt(baseConfig, IndexerSection, "VerifySimilarityPercentageTreshold");

            const string FoldersSection = "Folders";
            settings.WorkingFolder = GetSettingDirectoryInfo(baseConfig, FoldersSection, "Working");
            settings.BackupFolder = GetSettingDirectoryInfo(baseConfig, FoldersSection, "Backup");
            settings.PostFailedFolder = GetSettingDirectoryInfo(baseConfig, FoldersSection, "UploadFailed");
            settings.NzbOutputFolder = GetSettingDirectoryInfo(baseConfig, FoldersSection, "NzbOutput", true);

            const string PostingSection = "Posting";
            settings.MaxRepostCount = GetSettingInt(baseConfig, PostingSection, "MaxRepostCount");
            settings.RemoveAfterVerify = GetSettingBoolean(baseConfig, PostingSection, "RemoveAfterVerify");
            settings.RarNParSettings = GetSettingsRarNParSettings(baseConfig, PostingSection, "RarNParSettings");

            const string ExternalProgramsSection = "External Programs";
            settings.InactiveProcessTimeout = GetSettingInt(baseConfig, ExternalProgramsSection, "InactiveProcessTimeoutMinutes");
            settings.RarLocation = GetSettingString(baseConfig, ExternalProgramsSection, "RarLocation", true, "rar");
            settings.RarExtraParameters = GetSettingString(baseConfig, ExternalProgramsSection, "RarExtraParameters", true);
            settings.ParLocation = GetSettingString(baseConfig, ExternalProgramsSection, "ParLocation", true, "par2");
            settings.ParExtraParameters = GetSettingString(baseConfig, ExternalProgramsSection, "ParExtraParameters", true);
            settings.MkvPropEditLocation = GetSettingString(baseConfig, ExternalProgramsSection, "MkvPropEditLocation", true, "mkvpropedit");
            settings.FFmpegLocation = GetSettingString(baseConfig, ExternalProgramsSection, "FFmpegLocation", true, "ffmpeg");

            const string SubjobTimingSection = "Subjob Timings";
            settings.FilesystemCheckIntervalSeconds = GetSettingInt(baseConfig, SubjobTimingSection, "FilesystemCheckIntervalSeconds");
            settings.FilesystemCheckTesholdMinutes = GetSettingInt(baseConfig, SubjobTimingSection, "FilesystemCheckTesholdMinutes");
            settings.AutoposterIntervalSeconds = GetSettingInt(baseConfig, SubjobTimingSection, "AutoposterIntervalSeconds");
            settings.NotifierIntervalMinutes = GetSettingInt(baseConfig, SubjobTimingSection, "NotifierIntervalMinutes");
            settings.VerifierIntervalMinutes = GetSettingInt(baseConfig, SubjobTimingSection, "VerifierIntervalMinutes");
            settings.VerifyAfterMinutes = GetSettingInt(baseConfig, SubjobTimingSection, "VerifyAfterMinutes");
            settings.RepostAfterMinutes = GetSettingInt(baseConfig, SubjobTimingSection, "RepostAfterMinutes");
            settings.DatabaseCleanupHours = GetSettingInt(baseConfig, SubjobTimingSection, "DatabaseCleanupHours");
            settings.DatabaseCleanupKeepdays = GetSettingInt(baseConfig, SubjobTimingSection, "DatabaseCleanupKeepdays");

            const string NntpSection = "Nntp";
            settings.MaxRetryCount = GetSettingInt(baseConfig, NntpSection, "MaxRetryCount");
            settings.RetryDelaySeconds = GetSettingInt(baseConfig, NntpSection, "RetryDelaySeconds");
            settings.YEncLineSize = GetSettingInt(baseConfig, NntpSection, "YEncLineSize");
            settings.YEncLinesPerMessage = GetSettingInt(baseConfig, NntpSection, "YEncLinesPerMessage");

            return settings;
        }

        private static List<RarNParSetting> GetSettingsRarNParSettings(IConfigSource baseConfig, String section, String key)
        {
            List<RarNParSetting> rarNParSettings = new List<RarNParSetting>();
            String rarNParSettingsString = GetSettingString(baseConfig, section, key);

            try
            {
                String[] rarNParSettingsSplit1 = rarNParSettingsString.Split('|');
                foreach (String splitSection in rarNParSettingsSplit1)
                {
                    String[] rarNParSettingsSplit2 = splitSection.Split(',');
                    RarNParSetting setting = new RarNParSetting();
                    setting.FromSize = Int32.Parse(rarNParSettingsSplit2[0]);
                    setting.RarSize = Int32.Parse(rarNParSettingsSplit2[1]);
                    setting.Par2Percentage = Int32.Parse(rarNParSettingsSplit2[2]);
                    rarNParSettings.Add(setting);
                }
            }
            catch (Exception ex)
            {
                log.Fatal(String.Format("Could not parse Rar and Par settings. Is the string in the correct format ?"), ex);
                throw;
            }
            return rarNParSettings;
        }

        private static void LoadFolderConfigs(Settings settings)
        {
            Dictionary<String, IConfigSource> watchfolderConfigurations = new Dictionary<String, IConfigSource>();
            LoadIniFiles(watchfolderConfigurations);

            foreach(String watchfolderConfig in watchfolderConfigurations.Keys)
            {
                settings.WatchFolderSettings.Add(LoadWatchFolderSettingsFromIniFile(watchfolderConfigurations[watchfolderConfig]));
            }
        }

        private static void LoadIniFiles(Dictionary<string, IConfigSource> watchfolderConfigurations)
        {
            IConfigSource baseConfig = new IniConfigSource(new IniDocument("conf/watchfolders/default.ini", IniFileType.MysqlStyle));
            AddConfigAliasses(baseConfig);
            try
            {
                watchfolderConfigurations.Add(GetSettingString(baseConfig, WatchfolderSection, "ShortName"), baseConfig);
            }
            catch (Exception ex)
            {
                log.Fatal(String.Format("Could not load base watchfolder ini file. {0}", "conf/watchfolders/default.ini"), ex);
                throw;
            }

            List<String> userConfigFiles = new List<String>(Directory.GetFiles("userconf/watchfolders", "*.ini"));
            userConfigFiles.Sort();
            foreach (String filename in userConfigFiles)
            {
                try
                {
                    IConfigSource userConfig = new IniConfigSource(new IniDocument(filename, IniFileType.MysqlStyle));
                    AddConfigAliasses(userConfig);
                    String shortName = GetSettingString(userConfig, WatchfolderSection, "ShortName");
                    if (!watchfolderConfigurations.ContainsKey(shortName))
                    {
                        IConfigSource defaultConfig = new IniConfigSource(new IniDocument("conf/watchfolders/default.ini", IniFileType.MysqlStyle));
                        AddConfigAliasses(defaultConfig);
                        watchfolderConfigurations.Add(shortName, defaultConfig);
                    }
                    watchfolderConfigurations[shortName].Merge(userConfig);
                }
                catch (Exception ex)
                {
                    log.Fatal(String.Format("Could not load user watchfolder ini file. {0}", filename), ex);
                    throw;
                }
            }
        }

        private static WatchFolderSettings LoadWatchFolderSettingsFromIniFile(IConfigSource config)
        {
            WatchFolderSettings settings = new WatchFolderSettings();
            settings.ShortName = GetSettingString(config, WatchfolderSection, "ShortName");
            settings.Path = GetSettingDirectoryInfo(config, WatchfolderSection, "Path");
            settings.UseObfuscation = GetSettingBoolean(config, WatchfolderSection, "UseObfuscation");
            settings.CleanName = GetSettingBoolean(config, WatchfolderSection, "CleanName");
            settings.PreTag = GetSettingString(config, WatchfolderSection, "PreTag", true);
            settings.PostTag = GetSettingString(config, WatchfolderSection, "PostTag", true);
            settings.TargetNewsgroups = new List<String>(GetSettingString(config, WatchfolderSection, "TargetNewsgroups").Split('|'));
            settings.SpreadFilesOverTargetNewsgroups = GetSettingBoolean(config, WatchfolderSection, "SpreadFilesOverTargetNewsgroups");
            settings.StripFileMetadata = GetSettingBoolean(config, WatchfolderSection, "StripFileMetadata");
            settings.FromAddress = GetSettingString(config, WatchfolderSection, "FromAddress");
            settings.ApplyRandomPassword = GetSettingBoolean(config, WatchfolderSection, "ApplyRandomPassword");
            settings.RarPassword = GetSettingString(config, WatchfolderSection, "RarPassword", true);
            settings.Priority = GetSettingInt(config, WatchfolderSection, "Priority");

            return settings;
        }
        
        private static void AddConfigAliasses(IConfigSource config)
        {
            config.Alias.AddAlias("true", true);
            config.Alias.AddAlias("false", false);
            config.Alias.AddAlias("yes", true);
            config.Alias.AddAlias("no", false);
            config.Alias.AddAlias("1", true);
            config.Alias.AddAlias("0", false);
        }

        private static DirectoryInfo GetSettingDirectoryInfo(IConfigSource config, String section, String key, Boolean allowEmpty = false)
        {
            String directoryName = GetSettingString(config, section, key, allowEmpty);
            if(String.IsNullOrEmpty(directoryName)) //When we get empty string back allowempty will have been true so an extra check is not reauired.
            {
                return null;
            }

            try
            {
                return new DirectoryInfo(directoryName);
            }
            catch (Exception ex)
            {
                log.Fatal(String.Format("Fatal exception when reading setting from section [{0}] key [{1}] value [{2}]", section, key, directoryName), ex);
                throw;
            }
        }

        private static String GetSettingString(IConfigSource config, String section, String key, Boolean allowEmpty = false, String defaultValue = "")
        {
            if(config.Configs[section] == null)
            {
                log.FatalFormat("Config is missing [{0}] section.", section);
                throw new Exception(String.Format("Config is missing [{0}] section.", section));
            }

            String configValue = config.Configs[section].Get(key);

            if (String.IsNullOrWhiteSpace(configValue))
            {
                if (allowEmpty)
                {
                    return defaultValue;
                }
                else
                {
                    log.FatalFormat("Config is missing key [{0}] in section [{1}].", key, section);
                    throw new Exception(String.Format("Config is missing key [{0}] in section [{1}].", key, section));
                }
            }

            try
            {
                return config.Configs[section].GetExpanded(key);
            }
            catch (Exception ex)
            {
                log.Fatal(String.Format("Fatal exception when reading setting from section [{0}] key [{1}] value [{2}]", section, key, configValue), ex);
                throw;
            }            
        }

        private static Int32 GetSettingInt(IConfigSource config, String section, String key)
        {
            if (config.Configs[section] == null)
            {
                log.WarnFormat("Config is missing [{0}] section.", section);
                throw new Exception(String.Format("Config is missing [{0}] section.", section));
            }

            String configValue = config.Configs[section].Get(key);


            if (String.IsNullOrWhiteSpace(configValue))
            {
                log.FatalFormat("Config is missing key [{0}] in section [{1}].", key, section);
                throw new Exception(String.Format("Config is missing key [{0}] in section [{1}].", key, section));
            }

            try
            {
                return config.Configs[section].GetInt(key);
            }
            catch (Exception ex)
            {
                log.Fatal(String.Format("Fatal exception when reading setting from section [{0}] key [{1}] value [{2}]", section, key, configValue), ex);
                throw;
            }
        }

        private static Boolean GetSettingBoolean(IConfigSource config, String section, String key)
        {
            if (config.Configs[section] == null)
            {
                log.WarnFormat("Config is missing [{0}] section.", section);
                throw new Exception(String.Format("Config is missing [{0}] section.", section));
            }

            String configValue = config.Configs[section].Get(key);


            if (String.IsNullOrWhiteSpace(configValue))
            {
                log.FatalFormat("Config is missing key [{0}] in section [{1}].", key, section);
                throw new Exception(String.Format("Config is missing key [{0}] in section [{1}].", key, section));
            }

            try
            {
                return config.Configs[section].GetBoolean(key);
            }
            catch (Exception ex)
            {
                log.Fatal(String.Format("Fatal exception when reading setting from section [{0}] key [{1}] value [{2}]", section, key, configValue), ex);
                throw;
            }
        }
    }
}
