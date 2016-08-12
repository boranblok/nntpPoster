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
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static Settings LoadSettings()
        {
            var settings = GetSettingsFromIniFiles();
            settings.ValidateSettings();
            return settings;
        }

        private static void ValidateSettings(Settings settings)
        {
            if (settings.WatchFolderSettings.GroupBy(s => s.ShortName).Any(g => g.Count() > 1))
                throw new Exception("The watchfolder short name has to be unique.");

            if (settings.WatchFolderSettings.GroupBy(s => s.PathString).Any(g => g.Count() > 1))
                throw new Exception("The watchfolder path has to be unique.");

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

            if (settings.WatchFolderSettings.Any(s => s.ApplyRandomPassword) && String.IsNullOrWhiteSpace(NzbOutputFolderString))
            {
                log.Warn("ApplyRandomPassword is set to true for a watchfolder but NZB output folder is not set.");
                log.Warn("You will have to check the SQLite3 database to know what password was used for a release.");
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

        private static object GetSettingsFromIniFiles()
        {
            throw new NotImplementedException();
        }
    }
}
