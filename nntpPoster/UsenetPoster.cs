using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ExternalProcessWrappers;
using log4net;
using nntpPoster.yEncLib;
using Util;
using Util.Configuration;

namespace nntpPoster
{
    public class UsenetPoster
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<UploadSpeedReport> NewUploadSpeedReport;

        private Settings configuration;
        private WatchFolderSettings folderConfiguration;
        public UsenetPoster(Settings configuration, WatchFolderSettings folderConfiguration)
        {
            this.configuration = configuration;
            this.folderConfiguration = folderConfiguration;
        }
        
        private Int32 TotalPartCount { get; set; }
        private Object UploadLock = new Object();
        private Int32 UploadedPartCount { get; set; }
        private Int64 TotalUploadedBytes { get; set; }
        private DateTime UploadStartTime { get; set; }

        public XDocument PostToUsenet(FileSystemInfo toPost, String rarPassword, Boolean saveNzb = true)
        {
            return PostToUsenet(toPost, toPost.NameWithoutExtension(), rarPassword, saveNzb);
        }

        public XDocument PostToUsenet(FileSystemInfo toPost, String title, String rarPassword, Boolean saveNzb = true)
        {
            if(toPost.Size() == 0)
            {
                throw new Exception(String.Format("File/Folder [{0}] is 0 bytes in size, aborting post", toPost.FullName));
            }

            using (NntpMessagePoster poster = new NntpMessagePoster(configuration, folderConfiguration))
            {
                poster.PartPosted += Poster_PartPosted;
                DirectoryInfo processedFiles = null;
                try
                {
                    DateTime StartTime = DateTime.Now;
                    processedFiles = MakeProcessingFolder(toPost.NameWithoutExtension());
                    MakeRarAndParFiles(toPost, toPost.NameWithoutExtension(), processedFiles, rarPassword);

                    List<FileToPost> filesToPost = processedFiles.GetFiles()
                        .OrderBy(f => f.Name)
                        .Select(f => new FileToPost(configuration, folderConfiguration, f)).ToList();

                    List<PostedFileInfo> postedFiles = new List<PostedFileInfo>();
                    TotalPartCount = filesToPost.Sum(f => f.TotalParts);
                    UploadedPartCount = 0;
                    TotalUploadedBytes = 0;
                    UploadStartTime = DateTime.Now;

                    Int32 fileCount = 1;
                    foreach (var fileToPost in filesToPost)
                    {
                        DateTime filestartTime = DateTime.Now;
                        String comment1 = String.Format("{0} [{1}/{2}]", title, fileCount++, filesToPost.Count);
                        PostedFileInfo postInfo = fileToPost.PostYEncFile(poster, comment1, "");
                        if (log.IsInfoEnabled)
                        {
                            TimeSpan fileTimeElapsed = DateTime.Now - filestartTime;
                            Double fileSpeed = (Double)fileToPost.File.Length / fileTimeElapsed.TotalSeconds;
                            log.InfoFormat("Posted file {0} with a speed of {1}",
                                fileToPost.File.Name, UploadSpeedReport.GetHumanReadableSpeed(fileSpeed));
                        }
                        postedFiles.Add(postInfo);
                    }

                    poster.WaitTillCompletion();
                    if (log.IsInfoEnabled)
                    {
                        TimeSpan totalTimeElapsed = DateTime.Now - StartTime;
                        TimeSpan uploadTimeElapsed = DateTime.Now - UploadStartTime;
                        Double speed = TotalUploadedBytes / totalTimeElapsed.TotalSeconds;
                        Double ulSpeed = TotalUploadedBytes / uploadTimeElapsed.TotalSeconds;
                        log.InfoFormat("Upload of [{0}] has completed at {1} with an upload speed of {2}",
                            title, UploadSpeedReport.GetHumanReadableSpeed(speed), UploadSpeedReport.GetHumanReadableSpeed(ulSpeed));
                    }

                    Console.WriteLine();

                    XDocument nzbDoc = GenerateNzbFromPostInfo(toPost.Name, postedFiles, rarPassword);
                    if (saveNzb && configuration.NzbOutputFolder != null)
                        nzbDoc.Save(Path.Combine(configuration.NzbOutputFolder.FullName, toPost.NameWithoutExtension() + ".nzb"));
                    return nzbDoc;
                }
                finally
                {
                    if (processedFiles != null && processedFiles.Exists)
                    {
                        log.Info("Deleting processed folder");
                        processedFiles.Delete(true);
                    }
                }
            }
        }

        void Poster_PartPosted(object sender, YEncFilePart e)
        {
            Int32 _uploadedPartCount;
            Int64 _totalUploadedBytes;
            lock (UploadLock)
            {
                UploadedPartCount++;
                _uploadedPartCount = UploadedPartCount;
                TotalUploadedBytes += e.Size;
                _totalUploadedBytes = TotalUploadedBytes;
            }

            TimeSpan timeElapsed = DateTime.Now - UploadStartTime;
            Double speed = (Double) _totalUploadedBytes/timeElapsed.TotalSeconds;

            OnNewUploadSpeedReport(new UploadSpeedReport{
                    TotalParts = TotalPartCount,
                    UploadedParts = _uploadedPartCount,
                    BytesPerSecond = speed,
                    CurrentlyPostingName = e.SourcefileName
                });
        }

        protected virtual void OnNewUploadSpeedReport(UploadSpeedReport e)
        {
            Console.Write("\r" + e.ToString() + "   ");

            NewUploadSpeedReport?.Invoke(this, e);
        }

        private DirectoryInfo MakeProcessingFolder(String nameWithoutExtension)
        {
            var processedFolder = new DirectoryInfo(Path.Combine(
                configuration.WorkingFolder.FullName, nameWithoutExtension + "_readyToPost"));
            processedFolder.Create();
            return processedFolder;
        }

        private void MakeRarAndParFiles(FileSystemInfo toPost, String nameWithoutExtension,
            DirectoryInfo processedFolder, String password)
        {
            Int64 size = toPost.Size();
            var humanReadableSize = UploadSpeedReport.GetHumanReadableSize(size);
            log.DebugFormat("ToPost size: {0}({1})", size, humanReadableSize);
            var rarSizeRecommendation = configuration.RarNParSettings
                .Where(rr => rr.FromSizeBytes < size)
                .OrderByDescending(rr => rr.FromSize)
                .First();
            log.DebugFormat("Rar and par settings found: [{0},{1},{2}]", rarSizeRecommendation.FromSize, rarSizeRecommendation.RarSize, rarSizeRecommendation.Par2Percentage);
            var rarWrapper = new RarWrapper(configuration.InactiveProcessTimeout, configuration.RarLocation);
            DateTime rarStartTime = DateTime.Now;
            rarWrapper.Compress(
                toPost, 
                processedFolder, 
                nameWithoutExtension, 
                Settings.DetermineOptimalRarSize(rarSizeRecommendation.RarSize, 
                    configuration.YEncLineSize, configuration.YEncLinesPerMessage),
                password,
                configuration.RarExtraParameters);
            if (log.IsInfoEnabled)
            {
                TimeSpan rarTimeElapsed = DateTime.Now - rarStartTime;
                Double rarSpeed = (Double)size / rarTimeElapsed.TotalSeconds;
                log.InfoFormat("Rarred {0} of file(s) with a speed of {1}", humanReadableSize, UploadSpeedReport.GetHumanReadableSpeed(rarSpeed));
            }

            var parWrapper = new ParWrapper(configuration.InactiveProcessTimeout, configuration.ParLocation, configuration.ParCommandFormat);
            var processedSize = processedFolder.Size();
            Int32 partSize = parWrapper.CalculatePartSize(processedSize, configuration.YEncPartSize);
            DateTime parStartTime = DateTime.Now;
            Boolean makePar = true;
            while (makePar && partSize / configuration.YEncPartSize < 10)   //Max 10 loops.
            {
                try
                {
                    parWrapper.CreateParFilesInDirectory(
                        processedFolder, nameWithoutExtension, partSize, rarSizeRecommendation.Par2Percentage, configuration.ParExtraParameters);
                    makePar = false;
                }
                catch (Par2BlockSizeTooSmallException)
                {
                    partSize += configuration.YEncPartSize;
                }
            }
            if (log.IsInfoEnabled)
            {
                TimeSpan parTimeElapsed = DateTime.Now - parStartTime;
                Double parSpeed = (Double)size / parTimeElapsed.TotalSeconds;
                log.InfoFormat("Parred {0} of file(s) with a speed of {1}", UploadSpeedReport.GetHumanReadableSize(size), UploadSpeedReport.GetHumanReadableSpeed(parSpeed));
            }
        }

        private XDocument GenerateNzbFromPostInfo(String title, List<PostedFileInfo> postedFiles, String rarPassword)
        {
            XNamespace ns = "http://www.newzbin.com/DTD/2003/nzb";

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XDocumentType("nzb", "-//newzBin//DTD NZB 1.1//EN", "http://www.newzbin.com/DTD/nzb/nzb-1.1.dtd", null),
                new XElement(ns + "nzb",
                    new XElement(ns + "head",
                        new XElement(ns + "meta",
                            new XAttribute("type", "title"),
                            title
                            ),
                            String.IsNullOrWhiteSpace(rarPassword) ? null :
                                new XElement(ns + "meta",
                                    new XAttribute("type", "password"),
                                    rarPassword
                                )
                        ),
                    postedFiles.Select(f =>
                        new XElement(ns + "file",
                            new XAttribute("poster", f.FromAddress),
                            new XAttribute("date", f.GetUnixPostedDateTime()),
                            new XAttribute("subject", f.NzbSubjectName),
                            new XElement(ns + "groups",
                                f.PostedGroups.Select(g => new XElement(ns + "group", g))
                                ),
                            new XElement(ns + "segments",
                                f.Segments.OrderBy(s => s.SegmentNumber).Select(s =>
                                    new XElement(ns + "segment",
                                        new XAttribute("bytes", s.Bytes),
                                        new XAttribute("number", s.SegmentNumber),
                                        s.MessageIdWithoutBrackets
                                        )
                                    )
                                )
                            )
                        )
                    )
                );

            return doc;
        }
    }
}
