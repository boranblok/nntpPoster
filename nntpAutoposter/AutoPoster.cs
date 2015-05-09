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

namespace nntpAutoposter
{    
    class AutoPoster
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly String CharsToRemove = "()=@#$%^,?<>{}|";
        private static readonly String[] ffmpegHandledExtensions = new String[] {"mkv", "avi", "wmv", "mp4", 
                                                                                 "mov", "ogg", "ogm", "wav", 
                                                                                 "mka", "mks", "mpeg", "mpg", 
                                                                                 "vob", "mp3", "asf", "ape", "flac"};
        private Object monitor = new Object();
        private AutoPosterConfig configuration;
        private UsenetPosterConfig posterConfiguration;
        private UsenetPoster poster;
        private Task MyTask;
        private Boolean StopRequested;

        public AutoPoster(AutoPosterConfig configuration)
        {
            this.configuration = configuration;
            posterConfiguration = new UsenetPosterConfig();
            poster = new UsenetPoster(posterConfiguration);
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
            Console.WriteLine("Cleaning out processing folder of any leftover files.");
            foreach(var fsi in posterConfiguration.WorkingFolder.EnumerateFileSystemInfos())
            {
                FileAttributes attributes = File.GetAttributes(fsi.FullName);
                if (attributes.HasFlag(FileAttributes.Directory))
                    Directory.Delete(fsi.FullName, true);
                else
                    File.Delete(fsi.FullName);
            }
        }

        public void Stop()
        {
            lock (monitor)
            {
                StopRequested = true;
                Monitor.Pulse(monitor);
            }
            MyTask.Wait();
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
                if (nextUpload != null)
                {
                    FileSystemInfo toUpload;
                    Boolean isDirectory;
                    String fullPath = Path.Combine(configuration.BackupFolder.FullName, nextUpload.Name);
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
                        Console.WriteLine("Can no longer find {0} in the backup folder, cancelling upload", nextUpload.Name);
                        nextUpload.Cancelled = true;
                        DBHandler.Instance.UpdateUploadEntry(nextUpload);
                        return;
                    }
                    PostRelease(nextUpload, toUpload, isDirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("The upload failed to post. Retrying.");
                Console.WriteLine(ex.ToString());
            }
           
        }

        private void PostRelease(UploadEntry nextUpload, FileSystemInfo toUpload, Boolean isDirectory)
        {
            nextUpload.CleanedName = CleanName(toUpload.NameWithoutExtension()) + configuration.PostTag;
            if(configuration.UseObscufation)
                nextUpload.ObscuredName = Guid.NewGuid().ToString("N");

            FileSystemInfo toPost = null;
            try
            {
                if (isDirectory)
                {
                    toPost = PrepareDirectoryForPosting(nextUpload, (DirectoryInfo)toUpload);
                }
                else
                {
                    toPost = PrepareFileForPosting(nextUpload, (FileInfo)toUpload);
                }

                var nzbFile = poster.PostToUsenet(toPost, false);
                if (!String.IsNullOrWhiteSpace(posterConfiguration.NzbOutputFolder))
                    nzbFile.Save(Path.Combine(posterConfiguration.NzbOutputFolder, nextUpload.CleanedName + ".nzb"));

                nextUpload.UploadedAt = DateTime.UtcNow;
                DBHandler.Instance.UpdateUploadEntry(nextUpload);
            }
            finally
            {
                if(toPost != null)
                {
                    toPost.Refresh();
                    if(toPost.Exists)
                        toPost.Delete();
                }
            }
        }


        private DirectoryInfo PrepareDirectoryForPosting(UploadEntry nextUpload, DirectoryInfo toUpload)
        {
            String destination;
            if (configuration.UseObscufation)
                destination = Path.Combine(posterConfiguration.WorkingFolder.FullName, nextUpload.ObscuredName);
            else
                destination = Path.Combine(posterConfiguration.WorkingFolder.FullName, nextUpload.CleanedName);

            ((DirectoryInfo)toUpload).Copy(destination, true);
            return new DirectoryInfo(destination);
        }

        private FileInfo PrepareFileForPosting(UploadEntry nextUpload, FileInfo toUpload)
        {
            String destination;
            if (configuration.UseObscufation)
                destination = Path.Combine(posterConfiguration.WorkingFolder.FullName,
                    nextUpload.ObscuredName + toUpload.Extension);
            else
                destination = Path.Combine(posterConfiguration.WorkingFolder.FullName,
                    nextUpload.CleanedName + toUpload.Extension);


            ((FileInfo)toUpload).CopyTo(destination, true);
            FileInfo preparedFile = new FileInfo(destination);

            if(configuration.StripFileMetadata)
            {
                StripMetaDataFromFile(preparedFile);
            }

            return preparedFile;
        }

        private void StripMetaDataFromFile(FileInfo preparedFile)
        {
            if(preparedFile.Extension.Length < 1)
                return;

            String rawExt = preparedFile.Extension.Substring(1);
            if ("mkv".Equals(rawExt, StringComparison.InvariantCultureIgnoreCase))
                StripMkvMetaDataFromFile(preparedFile);
            if (ffmpegHandledExtensions.Any(ext => ext.Equals(rawExt, StringComparison.InvariantCultureIgnoreCase)))
                StripMetaDataWithFFmpeg(preparedFile);
        }

        private void StripMkvMetaDataFromFile(FileInfo preparedFile)
        {
            var mkvPropEdit = new MkvPropEditWrapper(posterConfiguration.InactiveProcessTimeout, configuration.MkvPropEditLocation);
            mkvPropEdit.SetTitle(preparedFile, "g33k");
        }

        private void StripMetaDataWithFFmpeg(FileInfo preparedFile)
        {
            var ffmpeg = new FFmpegWrapper(posterConfiguration.InactiveProcessTimeout, configuration.FFmpegLocation);
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
