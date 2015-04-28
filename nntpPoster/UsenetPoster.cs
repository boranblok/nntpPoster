using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using nntpPoster.yEncLib;
using par2Lib;
using rarLib;
using Util;

namespace nntpPoster
{
    public class UsenetPoster
    {
        public event EventHandler<UploadSpeedReport> NewUploadSpeedReport;

        private readonly UsenetPosterConfig _configuration;
        public UsenetPoster(UsenetPosterConfig configuration)
        {
            _configuration = configuration;
        }
        
        private Int32 TotalPartCount { get; set; }
        private readonly Object _uploadLock = new Object();
        private Int32 UploadedPartCount { get; set; }
        private Int64 TotalUploadedBytes { get; set; }
        private DateTime UploadStartTime { get; set; }

        public XDocument PostToUsenet(FileSystemInfo toPost, Boolean saveNzb = true)
        {
            return PostToUsenet(toPost, toPost.NameWithoutExtension(), saveNzb);
        }

        public XDocument PostToUsenet(FileSystemInfo toPost, String title, Boolean saveNzb = true)
        {
            var poster = new NntpMessagePoster(_configuration);
            poster.PartPosted += poster_PartPosted;
            var processedFiles = PrepareToPost(toPost);
            try
            {
                var filesToPost = processedFiles.GetFiles()
                    .OrderBy(f => f.Name)
                    .Select(f => new FileToPost(_configuration, f)).ToList();

                var postedFiles = new List<PostedFileInfo>();
                TotalPartCount = filesToPost.Sum(f => f.TotalParts);
                UploadedPartCount = 0;
                TotalUploadedBytes = 0;
                UploadStartTime = DateTime.Now;

                var fileCount = 1;
                foreach (var fileToPost in filesToPost)
                {
                    var comment1 = String.Format("{0} [{1}/{2}]", title, fileCount++, filesToPost.Count);
                    var postInfo = fileToPost.PostYEncFile(poster, comment1, "");
                    postedFiles.Add(postInfo);
                }

                poster.WaitTillCompletion();
                Console.WriteLine();

                var nzbDoc = GenerateNzbFromPostInfo(toPost.Name, postedFiles);
                if (saveNzb && !String.IsNullOrWhiteSpace(_configuration.NzbOutputFolder))
                    nzbDoc.Save(Path.Combine(_configuration.NzbOutputFolder, toPost.NameWithoutExtension() + ".nzb"));
                return nzbDoc;
            }
            finally
            {
                if (processedFiles.Exists)
                {
                    Console.WriteLine("Deleting processed folder");
                    processedFiles.Delete(true);
                }
            }
        }

        void poster_PartPosted(object sender, YEncFilePart e)
        {
            Int32 uploadedPartCount;
            Int64 totalUploadedBytes;
            lock (_uploadLock)
            {
                UploadedPartCount++;
                uploadedPartCount = UploadedPartCount;
                TotalUploadedBytes += e.Size;
                totalUploadedBytes = TotalUploadedBytes;
            }

            var timeElapsed = DateTime.Now - UploadStartTime;
            var speed = totalUploadedBytes/timeElapsed.TotalSeconds;

            OnNewUploadSpeedReport(new UploadSpeedReport{
                    TotalParts = TotalPartCount,
                    UploadedParts = uploadedPartCount,
                    BytesPerSecond = speed,
                    CurrentlyPostingName = e.SourcefileName
                });
        }

        protected virtual void OnNewUploadSpeedReport(UploadSpeedReport e)
        {
            Console.Write("\r" + e + "   ");

            var handler = NewUploadSpeedReport;
            if (handler != null) handler(this, e);
        }

        private DirectoryInfo PrepareToPost(FileSystemInfo toPost)
        {
            DirectoryInfo processedFolder = null;
            try
            {
                processedFolder = MakeRarAndParFiles(toPost, toPost.NameWithoutExtension());
                return processedFolder;
            }
            catch(Exception)
            {
                if (processedFolder != null && processedFolder.Exists)
                {
                    Console.WriteLine("Error occurred, deleting processed folder");
                    processedFolder.Delete(true);
                }
                throw;
            }
        }

        private DirectoryInfo MakeRarAndParFiles(FileSystemInfo toPost, String nameWithoutExtension)
        {
            var size = toPost.Size();
            var rarSizeRecommendation = _configuration.RecommendationMap
                .Where(rr => rr.FromFileSize < size)
                .OrderByDescending(rr => rr.FromFileSize)
                .First();
            var targetDirectory = new DirectoryInfo(Path.Combine(
                _configuration.WorkingFolder.FullName, nameWithoutExtension + "_readyToPost"));
            targetDirectory.Create();
            var rarWrapper = new RarWrapper(_configuration.RarLocation);
            rarWrapper.Compress(
                toPost, targetDirectory, nameWithoutExtension, rarSizeRecommendation.ReccomendedRarSize);

            var parWrapper = new ParWrapper(_configuration.ParLocation);
            parWrapper.CreateParFilesInDirectory(
                targetDirectory, nameWithoutExtension, _configuration.YEncPartSize, rarSizeRecommendation.ReccomendedRecoveryPercentage);

            return targetDirectory;
        }

        private XDocument GenerateNzbFromPostInfo(String title, List<PostedFileInfo> postedFiles)
        {
            XNamespace ns = "http://www.newzbin.com/DTD/2003/nzb";

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XDocumentType("nzb", "-//newzBin//DTD NZB 1.1//EN", "http://www.newzbin.com/DTD/nzb/nzb-1.1.dtd", null),
                new XElement(ns + "nzb",
                    new XElement(ns + "head",
                        new XElement(ns + "meta",
                            new XAttribute("type", "title"),
                            title
                            )
                        ),
                    postedFiles.Select(f =>
                        new XElement(ns + "file",
                            new XAttribute("poster", _configuration.FromAddress),
                            new XAttribute("date", f.PostedDateTime.GetUnixTimeStamp()),
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
