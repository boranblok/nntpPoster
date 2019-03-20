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
        public Int64 NotificationCount { get; set; }
        public Location CurrentLocation { get; set; }
        public Boolean HasNfo { get; set; }

        public void Move(Settings configuration, Location newLocation)
        {
            if(CurrentLocation == newLocation)
            {
                log.WarnFormat("Upload is already at the '{0}' location, cancelling move.", CurrentLocation);
                return;
            }
            
            DirectoryInfo targetFolder = DetermineTargetLocation(configuration, newLocation);
            String sourceFullPath = GetCurrentPath(configuration, Name);

            try
            {

                FileSystemInfo fso;
                FileAttributes attributes = File.GetAttributes(sourceFullPath);
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    fso = new DirectoryInfo(sourceFullPath);
                }
                else
                {
                    fso = new FileInfo(sourceFullPath);
                }

                var nameWithoutExtension = fso.NameWithoutExtension();

                fso.Move(targetFolder);

                if (HasNfo)
                {
                    try
                    {
                        String nfoFullPath = GetCurrentPath(configuration, nameWithoutExtension + ".nfo");
                        FileInfo sourceNfo = new FileInfo(nfoFullPath);
                        sourceNfo.Move(targetFolder);
                    }
                    catch (FileNotFoundException)
                    {
                        log.WarnFormat("Can no longer find the .nfo for this upload, removing HasNfo tag.");
                        HasNfo = false;
                    }
                }
            }
            catch (FileNotFoundException)
            {
                log.WarnFormat("Can no longer find '{0}', cancelling move.", sourceFullPath);
                CurrentLocation = Location.None;
            }
            CurrentLocation = newLocation;            
        }

        private DirectoryInfo DetermineTargetLocation(Settings configuration, Location newLocation)
        {
            switch(newLocation)
            {
                case Location.Queue:
                    return new DirectoryInfo(Path.Combine(configuration.QueueFolder.FullName, WatchFolderShortName));
                case Location.Backup:
                    return new DirectoryInfo(Path.Combine(configuration.BackupFolder.FullName, WatchFolderShortName));
                case Location.Failed:
                    return new DirectoryInfo(Path.Combine(configuration.PostFailedFolder.FullName, WatchFolderShortName));
            }
            throw new Exception("Target path can only be Queue, Backup or Failed");
        }

        public String GetCurrentPath(Settings configuration, String fileName)
        {
            switch(CurrentLocation)
            {
                case Location.Watch:
                    return Path.Combine(configuration.GetWatchFolderSettings(WatchFolderShortName).Path.FullName, fileName);
                case Location.Queue:
                    return Path.Combine(configuration.QueueFolder.FullName, WatchFolderShortName, fileName);
                case Location.Backup:
                    return Path.Combine(configuration.BackupFolder.FullName, WatchFolderShortName, fileName);
                case Location.Failed:
                    return Path.Combine(configuration.PostFailedFolder.FullName, WatchFolderShortName, fileName);
            }
            throw new Exception(String.Format("Current path of '{0}' cannot be determined.", fileName));
        }

        public void Delete(Settings configuration)
        {
            String fullPath = GetCurrentPath(configuration, Name);
            
            try
            {
                FileSystemInfo fso;
                FileAttributes attributes = File.GetAttributes(fullPath);
                var nameWithoutExtension = "";
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    nameWithoutExtension = new DirectoryInfo(fullPath).NameWithoutExtension();
                    Directory.Delete(fullPath, true);
                }
                else
                {
                    nameWithoutExtension = new FileInfo(fullPath).NameWithoutExtension();
                    File.Delete(fullPath);
                }

                if (HasNfo)
                {
                    try
                    {
                        String nfoFullPath = GetCurrentPath(configuration, nameWithoutExtension + ".nfo");
                        File.Delete(nfoFullPath);
                    }
                    catch (FileNotFoundException)
                    {
                        log.WarnFormat("Can no longer find the .nfo for this upload, removing HasNfo tag. cannot delete.");
                        HasNfo = false;
                    }
                }
            }
            catch (FileNotFoundException)
            {
                log.WarnFormat("Can no longer find '{0}', cannot delete.", fullPath);                
            }
            finally
            {
                CurrentLocation = Location.None;
            }
        }
    }
}
