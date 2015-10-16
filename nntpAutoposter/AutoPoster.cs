using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ExternalProcessWrappers;
using log4net;
using nntpPoster;
using Util;
using Util.Configuration;

namespace nntpAutoposter
{    
    public class AutoPoster
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly String CharsToRemove = "()=@#$%^,?<>{}|";
        private static readonly String[] ffmpegHandledExtensions = new String[] {"mkv", "avi", "wmv", "mp4", 
                                                                                 "mov", "ogg", "ogm", "wav", 
                                                                                 "mka", "mks", "mpeg", "mpg", 
                                                                                 "vob", "mp3", "asf", "ape", "flac"};
        private Object monitor = new Object();
        private Settings configuration;
        private Task MyTask;
        private Boolean StopRequested;

        public AutoPoster(Settings configuration)
        {
            this.configuration = configuration;
            StopRequested = false;
            MyTask = new Task(AutopostingTask, TaskCreationOptions.LongRunning);
        }

        public void Start()
        {
            InitializeEnvironment();
            MyTask.Start();
        }

        private void InitializeEnvironment()
        {
            log.Info("Cleaning out processing folder of any leftover files.");
            foreach(var fsi in configuration.WorkingFolder.EnumerateFileSystemInfos())
            {
                FileAttributes attributes = File.GetAttributes(fsi.FullName);
                if (attributes.HasFlag(FileAttributes.Directory))
                    Directory.Delete(fsi.FullName, true);
                else
                    File.Delete(fsi.FullName);
            }
        }

        public void Stop(Int32 millisecondsTimeout = Timeout.Infinite)
        {
            lock (monitor)
            {
                StopRequested = true;
                Monitor.Pulse(monitor);
            }
            MyTask.Wait(millisecondsTimeout);
        }

        private void AutopostingTask()
        {
            while(!StopRequested)
            {
                UploadNextItemInQueue();
                lock (monitor)
                {
                    if (StopRequested)
                    {
                        break;
                    }
                    Monitor.Wait(monitor, configuration.AutoposterIntervalMillis);
                }
            }
        }

        private void UploadNextItemInQueue()
        {
            try
            {
                UploadEntry nextUpload = DBHandler.Instance.GetNextUploadEntryToUpload();
                if (nextUpload == null) return;

                WatchFolderSettings folderConfiguration =
                    configuration.GetWatchFolderSettings(nextUpload.WatchFolderShortName);
                FileSystemInfo toUpload;
                Boolean isDirectory;
                String fullPath = Path.Combine(
                    configuration.BackupFolder.FullName, folderConfiguration.ShortName, nextUpload.Name);
                try
                {
                    FileAttributes attributes = File.GetAttributes(fullPath);
                    if (attributes.HasFlag(FileAttributes.Directory))
                    {
                        isDirectory = true;
                        toUpload = new DirectoryInfo(fullPath);
                    }
                    else
                    {
                        isDirectory = false;
                        toUpload = new FileInfo(fullPath);
                    }
                }
                catch (FileNotFoundException)
                {
                    log.WarnFormat("Can no longer find {0} in the backup folder, cancelling upload", nextUpload.Name);
                    nextUpload.Cancelled = true;
                    DBHandler.Instance.UpdateUploadEntry(nextUpload);
                    return;
                }
                if (nextUpload.UploadAttempts >= configuration.MaxRepostCount)
                {
                    log.WarnFormat("Cancelling the upload after {0} retry attempts.",
                        nextUpload.UploadAttempts);
                    nextUpload.Cancelled = true;
                    DirectoryInfo failedPostFolder = new DirectoryInfo(
                        Path.Combine(configuration.PostFailedFolderString, folderConfiguration.ShortName));
                    toUpload.Move(failedPostFolder);
                    DBHandler.Instance.UpdateUploadEntry(nextUpload);
                    return;
                }
                PostRelease(folderConfiguration, nextUpload, toUpload, isDirectory);
            }
            catch (Exception ex)
            {
                log.Error("The upload failed to post. Retrying.", ex);
            }
           
        }

        private void PostRelease(WatchFolderSettings folderConfiguration, UploadEntry nextUpload, FileSystemInfo toUpload, Boolean isDirectory)
        {
            nextUpload.UploadAttempts++;
            if (folderConfiguration.CleanName)
            {
                nextUpload.CleanedName = CleanName(toUpload.NameWithoutExtension()) + folderConfiguration.PostTag;
            }
            else
            {
                nextUpload.CleanedName = toUpload.NameWithoutExtension() + folderConfiguration.PostTag;
            }
            if (folderConfiguration.UseObfuscation)
            {
                nextUpload.ObscuredName = Guid.NewGuid().ToString("N");
                nextUpload.NotifiedIndexerAt = null;
            }
            DBHandler.Instance.UpdateUploadEntry(nextUpload);   //This ensures we already notify the indexer of our obfuscated post before we start posting.

            UsenetPoster poster = new UsenetPoster(configuration, folderConfiguration);
            FileSystemInfo toPost = null;
            try
            {
                if (isDirectory)
                {
                    toPost = PrepareDirectoryForPosting(folderConfiguration, nextUpload, (DirectoryInfo)toUpload);
                }
                else
                {
                    toPost = PrepareFileForPosting(folderConfiguration, nextUpload, (FileInfo)toUpload);
                }

                var nzbFile = poster.PostToUsenet(toPost, false);
                if (configuration.NzbOutputFolder != null)
                    nzbFile.Save(Path.Combine(configuration.NzbOutputFolder.FullName, nextUpload.CleanedName + ".nzb"));

                nextUpload.UploadedAt = DateTime.UtcNow;
                DBHandler.Instance.UpdateUploadEntry(nextUpload);
            }
            finally
            {
                if(toPost != null)
                {
                    toPost.Refresh();
                    if(toPost.Exists)
                    {
                        FileAttributes attributes = File.GetAttributes(toPost.FullName);
                        if (attributes.HasFlag(FileAttributes.Directory))
                        {
                            Directory.Delete(toPost.FullName, true);
                        }
                        else
                        {
                            File.Delete(toPost.FullName);
                        }
                    }
                }
            }
        }


        private DirectoryInfo PrepareDirectoryForPosting(WatchFolderSettings folderConfiguration, 
            UploadEntry nextUpload, DirectoryInfo toUpload)
        {
            String destination;
            if (folderConfiguration.UseObfuscation)
                destination = Path.Combine(configuration.WorkingFolder.FullName, folderConfiguration.ShortName, nextUpload.ObscuredName);
            else
                destination = Path.Combine(configuration.WorkingFolder.FullName, folderConfiguration.ShortName, nextUpload.CleanedName);

            if (!Directory.Exists(Path.Combine(configuration.WorkingFolder.FullName, folderConfiguration.ShortName)))
                Directory.CreateDirectory(Path.Combine(configuration.WorkingFolder.FullName, folderConfiguration.ShortName));

            ((DirectoryInfo)toUpload).Copy(destination, true);
            return new DirectoryInfo(destination);
        }

        private FileInfo PrepareFileForPosting(WatchFolderSettings folderConfiguration, UploadEntry nextUpload, FileInfo toUpload)
        {
            String destination;
            if (folderConfiguration.UseObfuscation)
                destination = Path.Combine(configuration.WorkingFolder.FullName,
                    folderConfiguration.ShortName,
                    nextUpload.ObscuredName + toUpload.Extension);
            else
                destination = Path.Combine(configuration.WorkingFolder.FullName,
                    folderConfiguration.ShortName,
                    nextUpload.CleanedName + toUpload.Extension);

            if (!Directory.Exists(Path.Combine(configuration.WorkingFolder.FullName, folderConfiguration.ShortName)))
                Directory.CreateDirectory(Path.Combine(configuration.WorkingFolder.FullName, folderConfiguration.ShortName));


            ((FileInfo)toUpload).CopyTo(destination, true);
            FileInfo preparedFile = new FileInfo(destination);

            if(folderConfiguration.StripFileMetadata)
            {
                StripMetaDataFromFile(preparedFile);
            }

            return preparedFile;
        }

        private void StripMetaDataFromFile(FileInfo preparedFile)
        {
            try
            {
                if (preparedFile.Extension.Length < 1)
                    return;

                String rawExt = preparedFile.Extension.Substring(1);
                if ("mkv".Equals(rawExt, StringComparison.InvariantCultureIgnoreCase))
                    StripMkvMetaDataFromFile(preparedFile);
                if (ffmpegHandledExtensions.Any(ext => ext.Equals(rawExt, StringComparison.InvariantCultureIgnoreCase)))
                    StripMetaDataWithFFmpeg(preparedFile);
            }
            catch(Exception ex)
            {
                log.Warn("Could not strip metadata from file. Posting with metadata.", ex);
            }
        }

        private void StripMkvMetaDataFromFile(FileInfo preparedFile)
        {
            var mkvPropEdit = new MkvPropEditWrapper(configuration.InactiveProcessTimeout, configuration.MkvPropEditLocation);
            mkvPropEdit.SetTitle(preparedFile, "g33k");
        }

        private void StripMetaDataWithFFmpeg(FileInfo preparedFile)
        {
            var ffmpeg = new FFmpegWrapper(configuration.InactiveProcessTimeout, configuration.FFmpegLocation);
            ffmpeg.TryStripMetadata(preparedFile);
        }

        private String CleanName(String nameToClean)
        {
            String cleanName = Regex.Replace(nameToClean, "^[:ascii:]", String.Empty);
            cleanName = cleanName.Replace(' ', '.');
            cleanName = cleanName.Replace("+", ".");
            cleanName = cleanName.Replace("&", "and");
            foreach(var charToRemove in CharsToRemove)
            {
                cleanName = cleanName.Replace(charToRemove.ToString(), String.Empty);
            }

            cleanName = Regex.Replace(cleanName, "\\.{2,}", String.Empty);

            return cleanName;
        }
    }
}
