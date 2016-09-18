using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using nntpPoster.yEncLib;
using Util.Configuration;

namespace nntpPoster
{
    public class FileToPost
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Settings configuration;
        private WatchFolderSettings folderConfiguration;
        private Int32 partSize;
        public FileInfo File { get; private set; }

        public Int32 TotalParts { get; private set; }

        public FileToPost(Settings configuration, WatchFolderSettings folderConfiguration, FileInfo fileToPost)
        {
            this.configuration = configuration;
            this.folderConfiguration = folderConfiguration;
            partSize = configuration.YEncPartSize;

            File = fileToPost;
            DetermineTotalParts();
        }

        private string ConstructSubjectNameBase(String prefix, String suffix)
        {
            StringBuilder subject = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(prefix))
                subject.Append(prefix + " ");
            subject.AppendFormat("\"" + File.Name + "\"");
            subject.Append(" yEnc ");
            subject.Append("({0}/");    //The {0}  placeholder here is for the format statement later.
            subject.Append(TotalParts);
            subject.Append(")");
            if (!String.IsNullOrWhiteSpace(suffix))
                subject.Append(" " + suffix);

            return subject.ToString();
        }

        private void DetermineTotalParts()
        {
            TotalParts = (Int32)(File.Length / partSize);
            if (File.Length % partSize > 0)
                TotalParts += 1;
        }

        public PostedFileInfo PostYEncFile(InntpMessagePoster poster, String prefix, String suffix)
        {
            PostedFileInfo postedFileInfo = new PostedFileInfo();
            String subjectNameBase = ConstructSubjectNameBase(prefix, suffix);
            postedFileInfo.NzbSubjectName = String.Format(subjectNameBase, 1);
            postedFileInfo.PostedGroups.AddRange(folderConfiguration.GetTargetNewsGroups());
            postedFileInfo.PostedDateTime = DateTime.Now;

            var yEncoder = new YEncEncoder();
            Int32 partNumber = 0;

            using (var fileStream = File.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                while (fileStream.Position < fileStream.Length - 1) //If we have more blocks to encode.
                {
                    partNumber++;
                    Byte[] partBuffer = new Byte[partSize];
                    Int32 partBufferPos = 0;
                    Int32 bytesRead = 0;
                    CRC32 partCRCCalculator = new CRC32();

                    var part = new YEncFilePart();
                    part.SourcefileName = File.Name;
                    part.Number = partNumber;
                    part.Begin = fileStream.Position + 1;

                    while (partBufferPos < partSize - 1 &&
                        (bytesRead = fileStream.Read(partBuffer, partBufferPos, partSize - partBufferPos)) > 0)
                    {
                        partBufferPos += bytesRead;
                    }

                    //TODO: this can be split in 2 threads to spread out CPU usage.
                    part.EncodedLines = yEncoder.EncodeBlock(configuration.YEncLineSize, partBuffer, 0, partBufferPos);

                    partCRCCalculator.TransformFinalBlock(partBuffer, 0, partBufferPos);
                    part.CRC32 = partCRCCalculator.HashAsHexString;

                    part.End = fileStream.Position;

                    PostPart(poster, postedFileInfo, part, subjectNameBase);
                }
            }

            return postedFileInfo;
        }

        private void PostPart(InntpMessagePoster poster, PostedFileInfo postedFileInfo, YEncFilePart part, 
            String subjectNameBase)
        {
            var message = new nntpMessage();
            message.Subject = String.Format(subjectNameBase, part.Number);

            message.YEncFilePart = part;
            message.PostInfo = postedFileInfo;

            if (TotalParts > 1)
            {
                message.Prefix.Add(String.Format("=ybegin part={0} total={1} line={2} size={3} name={4}",
                    part.Number, TotalParts, configuration.YEncLineSize, File.Length, File.Name));

                message.Prefix.Add(String.Format("=ypart begin={0} end={1}",
                    part.Begin, part.End));

                message.Suffix.Add(String.Format("=yend size={0} part={1} pcrc32={2}",
                    part.Size, part.Number, part.CRC32));

            }
            else
            {
                message.Prefix.Add(String.Format("=ybegin line={0} size={1} name={2}",
                    configuration.YEncLineSize, File.Length, File.Name));

                message.Suffix.Add(String.Format("=yend size={0} crc32={1}",
                    File.Length, part.CRC32));
            }

            poster.PostMessage(message);
        }
    }
}
