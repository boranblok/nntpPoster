using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ffmpegLib;
using mkvPropEditLib;
using nntpPoster;
using Util;

namespace nntpAutoposter
{    
    class AutoPoster
    {
        private const String CharsToRemove = "()=@#$%^+,?<>{}|";

        private static readonly String[] FfmpegHandledExtensions =
        {"mkv", "avi", "wmv", "mp4", 
            "mov", "ogg", "ogm", "wav", 
            "mka", "mks", "mpeg", "mpg", 
            "vob", "mp3", "asf", "ape", "flac"};
        private readonly Object _monitor = new Object();
        private readonly AutoPosterConfig _configuration;
        private readonly UsenetPosterConfig _posterConfiguration;
        private readonly UsenetPoster _poster;
        private readonly Task _myTask;
        private Boolean _stopRequested;

        public AutoPoster(AutoPosterConfig configuration)
        {
            _configuration = configuration;
            _posterConfiguration = new UsenetPosterConfig();
            _poster = new UsenetPoster(_posterConfiguration);
            _stopRequested = false;
            _myTask = new Task(AutopostingTask, TaskCreationOptions.LongRunning);
        }

        public void Start()
        {
            InitializeEnvironment();
            _myTask.Start();
        }

        private void InitializeEnvironment()
        {
            Console.WriteLine("Cleaning out processing folder of any leftover files.");
            foreach(var fsi in _posterConfiguration.WorkingFolder.EnumerateFileSystemInfos())
            {
                var attributes = File.GetAttributes(fsi.FullName);
                if (attributes.HasFlag(FileAttributes.Directory))
                    Directory.Delete(fsi.FullName, true);
                else
                    File.Delete(fsi.FullName);
            }
        }

        public void Stop()
        {
            lock (_monitor)
            {
                _stopRequested = true;
                Monitor.Pulse(_monitor);
            }
            _myTask.Wait();
        }

        private void AutopostingTask()
        {
            while(!_stopRequested)
            {
                UploadNextItemInQueue();
                lock (_monitor)
                {
                    if (_stopRequested)
                    {
                        break;
                    }
                    Monitor.Wait(_monitor, _configuration.AutoposterIntervalMillis);
                }
            }
        }

        private void UploadNextItemInQueue()
        {
            var nextUpload = DbHandler.Instance.GetNextUploadEntryToUpload();
            if (nextUpload != null)
            {
                FileSystemInfo toUpload;
                Boolean isDirectory;
                var fullPath = Path.Combine(_configuration.BackupFolder.FullName, nextUpload.Name);
                try
                {
                    var attributes = File.GetAttributes(fullPath);
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
                catch(FileNotFoundException)
                {
                    Console.WriteLine("Can no longer find {0} in the backup folder, cancelling upload", nextUpload.Name);
                    nextUpload.Cancelled = true;
                    DbHandler.Instance.UpdateUploadEntry(nextUpload);
                    return;
                }
                PostRelease(nextUpload, toUpload, isDirectory);
            }
        }

        private void PostRelease(UploadEntry nextUpload, FileSystemInfo toUpload, Boolean isDirectory)
        {
            nextUpload.CleanedName = CleanName(toUpload.NameWithoutExtension()) + _configuration.PostTag;
            if(_configuration.UseObscufation)
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

                var nzbFile = _poster.PostToUsenet(toPost, false);
                if (!String.IsNullOrWhiteSpace(_posterConfiguration.NzbOutputFolder))
                    nzbFile.Save(Path.Combine(_posterConfiguration.NzbOutputFolder, nextUpload.CleanedName + ".nzb"));

                nextUpload.UploadedAt = DateTime.UtcNow;
                DbHandler.Instance.UpdateUploadEntry(nextUpload);
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
            var destination = Path.Combine(_posterConfiguration.WorkingFolder.FullName, 
                _configuration.UseObscufation ? nextUpload.ObscuredName : nextUpload.CleanedName);

            toUpload.Copy(destination, true);
            return new DirectoryInfo(destination);
        }

        private FileInfo PrepareFileForPosting(UploadEntry nextUpload, FileInfo toUpload)
        {
            String destination;
            if (_configuration.UseObscufation)
                destination = Path.Combine(_posterConfiguration.WorkingFolder.FullName,
                    nextUpload.ObscuredName + toUpload.Extension);
            else
                destination = Path.Combine(_posterConfiguration.WorkingFolder.FullName,
                    nextUpload.CleanedName + toUpload.Extension);


            toUpload.CopyTo(destination, true);
            var preparedFile = new FileInfo(destination);

            if(_configuration.StripFileMetadata)
            {
                StripMetaDataFromFile(preparedFile);
            }

            return preparedFile;
        }

        private void StripMetaDataFromFile(FileInfo preparedFile)
        {
            if(preparedFile.Extension.Length < 1)
                return;

            var rawExt = preparedFile.Extension.Substring(1);
            if ("mkv".Equals(rawExt, StringComparison.InvariantCultureIgnoreCase))
                StripMkvMetaDataFromFile(preparedFile);
            if (FfmpegHandledExtensions.Any(ext => ext.Equals(rawExt, StringComparison.InvariantCultureIgnoreCase)))
                StripMetaDataWithFFmpeg(preparedFile);
        }

        private void StripMkvMetaDataFromFile(FileInfo preparedFile)
        {
            var mkvPropEdit = new MkvPropEditWrapper(_configuration.MkvPropEditLocation);
            mkvPropEdit.SetTitle(preparedFile, "g33k");
        }

        private void StripMetaDataWithFFmpeg(FileInfo preparedFile)
        {
            var ffmpeg = new FFmpegWrapper(_configuration.FFmpegLocation);
            ffmpeg.TryStripMetadata(preparedFile);
        }

        private String CleanName(String nameToClean)
        {
            var cleanName = Regex.Replace(nameToClean, "^[:ascii:]", String.Empty);
            cleanName = cleanName.Replace(' ', '.');
            cleanName = cleanName.Replace("&", "and");
            foreach(var charToRemove in CharsToRemove)
            {
                cleanName = cleanName.Replace(
                    charToRemove.ToString(CultureInfo.InvariantCulture), String.Empty);
            }

            cleanName = Regex.Replace(cleanName, "\\.{2,}", String.Empty);

            return cleanName;
        }
    }
}
