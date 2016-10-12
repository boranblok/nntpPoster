using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;
using Util.Configuration;

namespace nntpAutoposter
{
    public class UploadEntry
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Int64 ID { get; set; }
        public String Name { get; set; }
        public Int64 Size { get; set; }
        public String CleanedName { get; set; }
        public String ObscuredName { get; set; }
        public Boolean RemoveAfterVerify { get; set; }
        public DateTime CreatedAt { get; set; }
        public Nullable<DateTime> UploadedAt { get; set; }
        public Nullable<DateTime> NotifiedIndexerAt { get; set; }
        public Nullable<DateTime> SeenOnIndexAt { get; set; }
        public Boolean Cancelled { get; set; }
        public String WatchFolderShortName { get; set; }
        public Int64 UploadAttempts { get; set; }
        public String RarPassword { get; set; }
        public Int64 PriorityNum { get; set; }
        public String NzbContents { get; set; }
        public Boolean IsRepost { get; set; }

        public void MoveToFailedFolder(Settings config)
        {
            String fullPath = Path.Combine(config.BackupFolder.FullName, WatchFolderShortName, Name);

            FileSystemInfo fso;

            try
            {
                FileAttributes attributes = File.GetAttributes(fullPath);
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    fso = new DirectoryInfo(fullPath);
                }
                else
                {
                    fso = new FileInfo(fullPath);
                }

                DirectoryInfo failedPostFolder = new DirectoryInfo(
                Path.Combine(config.PostFailedFolder.FullName, WatchFolderShortName));
                fso.Move(failedPostFolder);
            }
            catch (FileNotFoundException)
            {
                log.WarnFormat("Can no longer find {0} in the backup folder, cancelling move.", Name);
            }
        }

        public void DeleteBackup(Settings config)
        {
            String fullPath = Path.Combine(config.BackupFolder.FullName, WatchFolderShortName, Name);
            
            try
            {
                FileAttributes attributes = File.GetAttributes(fullPath);
                if (attributes.HasFlag(FileAttributes.Directory))
                    Directory.Delete(fullPath, true);
                else
                    File.Delete(fullPath);
            }
            catch (FileNotFoundException)
            {
                log.WarnFormat("Can no longer find {0} in the backup folder, cannot delete.", Name);
            }
        }
    }
}
