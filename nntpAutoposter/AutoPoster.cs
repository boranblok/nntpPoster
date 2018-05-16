﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
using System.Xml.Linq;

namespace nntpAutoposter
{    
    public class AutoPoster
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            log.InfoFormat("Starting nntpPoster version {0}", Assembly.GetExecutingAssembly().GetName().Version);
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
                    Monitor.Wait(monitor, new TimeSpan(0, 0, configuration.AutoposterIntervalSeconds));
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
                String fullPath = nextUpload.GetCurrentPath(configuration);
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
                    log.WarnFormat("Can no longer find '{0}', cancelling upload", fullPath);
                    nextUpload.CurrentLocation = Location.None;
                    nextUpload.Cancelled = true;
                    DBHandler.Instance.UpdateUploadEntry(nextUpload);
                    return;
                }
                if (nextUpload.UploadAttempts >= configuration.MaxRepostCount)
                {
                    log.WarnFormat("Cancelling the upload after {0} retry attempts.",
                        nextUpload.UploadAttempts);
                    nextUpload.Cancelled = true;
                    nextUpload.Move(configuration, Location.Failed);
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
                nextUpload.CleanedName = ApplyTags(CleanName(folderConfiguration, StripNonAscii(toUpload.NameWithoutExtension())), folderConfiguration);
            }
            else
            {
                nextUpload.CleanedName = ApplyTags(StripNonAscii(toUpload.NameWithoutExtension()), folderConfiguration);
            }
            if (folderConfiguration.UseObfuscation)
            {
                nextUpload.ObscuredName = Guid.NewGuid().ToString("N");
                nextUpload.NotifiedIndexerAt = null;
            }
            DBHandler.Instance.UpdateUploadEntry(nextUpload);

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

                String password = folderConfiguration.RarPassword;
                if (folderConfiguration.ApplyRandomPassword)
                    password = Guid.NewGuid().ToString("N");

                var nzbFile = poster.PostToUsenet(toPost, password, false);
                
                nextUpload.NzbContents = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + nzbFile.ToString();

                if (configuration.NzbOutputFolder != null)
                {
                    FileInfo file = new FileInfo(Path.Combine(configuration.NzbOutputFolder.FullName, nextUpload.CleanedName + ".nzb"));
                    File.WriteAllText(file.FullName, nextUpload.NzbContents);
                }

                nextUpload.RarPassword = password;
                nextUpload.UploadedAt = DateTime.UtcNow;
                nextUpload.Move(configuration, Location.Backup);
                DBHandler.Instance.UpdateUploadEntry(nextUpload);
                log.InfoFormat("[{0}] was uploaded as obfuscated release [{1}] to usenet."
                    , nextUpload.CleanedName, nextUpload.ObscuredName);
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

        private String ApplyTags(String cleanedName, WatchFolderSettings folderConfiguration)
        {
            if (!cleanedName.StartsWith(folderConfiguration.PreTag))
                cleanedName = folderConfiguration.PreTag + cleanedName;

            if (!cleanedName.EndsWith(folderConfiguration.PostTag))
                cleanedName = cleanedName + folderConfiguration.PostTag;

            return cleanedName;
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
            mkvPropEdit.SetTitle(preparedFile, String.Empty);
        }

        private void StripMetaDataWithFFmpeg(FileInfo preparedFile)
        {
            var ffmpeg = new FFmpegWrapper(configuration.InactiveProcessTimeout, configuration.FFmpegLocation);
            ffmpeg.TryStripMetadata(preparedFile);
        }

        private String CleanName(WatchFolderSettings folderConfiguration, String nameToClean)
        {
            String cleanName = nameToClean.Replace(' ', '.');
            cleanName = cleanName.Replace("+", ".");
            cleanName = cleanName.Replace("&", "and");
            foreach(var charToRemove in folderConfiguration.CharsToRemove)
            {
                cleanName = cleanName.Replace(charToRemove.ToString(), String.Empty);
            }

            cleanName = Regex.Replace(cleanName, "\\.{2,}", String.Empty);

            log.InfoFormat("Cleaned the name [{0}] to [{1}]", nameToClean, cleanName);
            return cleanName;
        }

        static string StripNonAscii(String toStrip)
        {
            StringBuilder buffer = new StringBuilder(toStrip.Length); //Max length
            foreach (char ch in toStrip)
            {
                UInt16 charNum = Convert.ToUInt16(ch);//In .NET, chars are UTF-16
                //The basic characters have the same code points as ASCII, and the extended characters are bigger
                if ((charNum >= 32u) && (charNum <= 126u)) buffer.Append(ch);
            }
            return buffer.ToString();
        }
    }
}
