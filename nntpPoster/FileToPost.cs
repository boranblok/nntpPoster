using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nntpPoster.yEncLib;

namespace nntpPoster
{
    public class FileToPost
    {
        private UsenetPosterConfig configuration;
        private Int32 partSize;
        private FileInfo file;

        public Int32 TotalParts { get; private set; }

        public FileToPost(UsenetPosterConfig configuration, FileInfo fileToPost)
        {
            this.configuration = configuration;
            partSize = configuration.YEncPartSize;

            file = fileToPost;
            DetermineTotalParts();
        }

        private string ConstructSubjectNameBase(String prefix, String suffix)
        {
            StringBuilder subject = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(prefix))
                subject.Append(prefix + " ");
            subject.AppendFormat("\"" + file.Name + "\"");
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
            TotalParts = (Int32)(file.Length / partSize);
            if (file.Length % partSize > 0)
                TotalParts += 1;
        }

        public PostedFileInfo PostYEncFile(InntpMessagePoster poster, String prefix, String suffix)
        {
            PostedFileInfo postedFileInfo = new PostedFileInfo();
            String subjectNameBase = ConstructSubjectNameBase(prefix, suffix);
            postedFileInfo.NzbSubjectName = String.Format(subjectNameBase, 1);
            postedFileInfo.PostedGroups.AddRange(configuration.TargetNewsgroups);
            postedFileInfo.PostedDateTime = DateTime.Now;

            var yEncoder = new YEncEncoder();
            Int32 partNumber = 0;

            using (var fileStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                while (fileStream.Position < fileStream.Length - 1) //If we have more blocks to encode.
                {
                    partNumber++;
                    Byte[] partBuffer = new Byte[partSize];
                    Int32 partBufferPos = 0;
                    Int32 bytesRead = 0;
                    CRC32 partCRCCalculator = new CRC32();

                    var part = new YEncFilePart();
                    part.SourcefileName = file.Name;
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
                    part.Number, TotalParts, configuration.YEncLineSize, file.Length, file.Name));

                message.Prefix.Add(String.Format("=ypart begin={0} end={1}",
                    part.Begin, part.End));

                message.Suffix.Add(String.Format("=yend size={0} part={1} pcrc32={2}",
                    part.Size, part.Number, part.CRC32));

            }
            else
            {
                message.Prefix.Add(String.Format("=ybegin line={0} size={1} name={2}",
                    configuration.YEncLineSize, file.Length, file.Name));

                message.Suffix.Add(String.Format("=yend size={0} crc32={1}",
                    file.Length, part.CRC32));
            }

            poster.PostMessage(message);
        }
    }
}
