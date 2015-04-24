using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using nntpPoster.yEncLib;
using par2Lib;
using rarLib;

namespace nntpPoster
{
    public class UsenetPoster
    {
        public event EventHandler<UploadSpeedReport> newUploadSpeedReport;

        private UsenetPosterConfig configuration;
        public UsenetPoster(UsenetPosterConfig configuration)
        {
            this.configuration = configuration;
        }

        
        private Int32 TotalPartCount { get; set; }
        private Object UploadLock = new Object();
        private Int32 UploadedPartCount { get; set; }
        private Int64 TotalUploadedBytes { get; set; }
        private DateTime UploadStartTime { get; set; }

        public XDocument PostToUsenet(FileSystemInfo toPost)
        {
            nntpMessagePoster poster = new nntpMessagePoster(configuration);
            poster.PartPosted += poster_PartPosted;
            DirectoryInfo processedFiles = PrepareToPost(toPost);
            try
            {
                List<FileToPost> filesToPost = processedFiles.GetFiles()
                    .Select(f => new FileToPost(configuration, f)).ToList();

                List < PostedFileInfo > postedFiles = new List<PostedFileInfo>();
                TotalPartCount = filesToPost.Sum(f => f.TotalParts);
                UploadedPartCount = 0;
                TotalUploadedBytes = 0;
                UploadStartTime = DateTime.Now;

                Int32 fileCount = 1;
                foreach (var fileToPost in filesToPost)
                {
                    String comment1 = String.Format("{0}/{1}", fileCount++, filesToPost.Count);
                    PostedFileInfo postInfo = fileToPost.PostYEncFile(poster, comment1, "");
                    postedFiles.Add(postInfo);
                }

                poster.WaitTillCompletion();

                XDocument nzbDoc = GenerateNzbFromPostInfo(toPost.Name, postedFiles);
                if (!String.IsNullOrWhiteSpace(configuration.NzbOutputFolder))
                    nzbDoc.Save(Path.Combine(configuration.NzbOutputFolder, toPost.NameWithoutExtension() + ".nzb"));
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
                    BytesPerSecond = speed
                });
        }

        protected virtual void OnNewUploadSpeedReport(UploadSpeedReport e)
        {
            EventHandler<UploadSpeedReport> handler = newUploadSpeedReport;
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
            Int64 size = toPost.Size();
            var rarSizeRecommendation = configuration.RecommendationMap
                .Where(rr => rr.FromFileSize < size)
                .OrderByDescending(rr => rr.FromFileSize)
                .First();
            DirectoryInfo targetDirectory = new DirectoryInfo(Path.Combine(
                configuration.WorkingFolder.FullName, nameWithoutExtension));
            targetDirectory.Create();
            var rarWrapper = new RarWrapper(configuration.RarToolLocation);
            rarWrapper.Compress(
                toPost, targetDirectory, nameWithoutExtension, rarSizeRecommendation.ReccomendedRarSize);

            var parWrapper = new ParWrapper(configuration.ParToolLocation);
            parWrapper.CreateParFilesInDirectory(
                targetDirectory, configuration.YEncPartSize, rarSizeRecommendation.ReccomendedRecoveryPercentage);

            return targetDirectory;
        }

        private XDocument GenerateNzbFromPostInfo(String title, List<PostedFileInfo> postedFiles)
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
                            )
                        ),
                    postedFiles.Select(f =>
                        new XElement(ns + "file",
                            new XAttribute("poster", configuration.FromAddress),
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
