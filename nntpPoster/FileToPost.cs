using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NntpClientLib;
using nntpPoster.yEncLib;

namespace nntpPoster
{
    public class FileToPost
    {
        private UsenetPosterConfig configuration;
        private Int32 partSize;
        private FileInfo file;

        public String FileName { get; private set; }
        public Int32 TotalParts { get; private set; }

        public FileToPost(UsenetPosterConfig configuration, FileInfo fileToPost)
        {
            this.configuration = configuration;
            partSize = configuration.YEncPartSize;

            file = fileToPost;
            DetermineFileName();
            DetermineTotalParts();
        }

        private void DetermineFileName()
        {
            FileName = Regex.Replace(file.Name, "^[:ascii:]", String.Empty);
            FileName = FileName.Replace(' ', '.');
        }


        private string ConstructSubjectNameBase(String comment1, String comment2)
        {
            StringBuilder subject = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(comment1))
                subject.AppendFormat("[{0}] ", comment1);
            subject.AppendFormat("\"" + FileName + "\"");
            subject.Append(" yEnc ");
            if (TotalParts > 1)
            {
                subject.Append("({0}/");    //The {0}  placeholder here is for the format statement later.
                subject.Append(TotalParts);
                subject.Append(") ");
            }
            subject.Append(file.Length);
            if (!String.IsNullOrWhiteSpace(comment2))
                subject.AppendFormat(" [{0}]", comment2);

            return subject.ToString();
        }

        private void DetermineTotalParts()
        {
            TotalParts = (Int32)(file.Length / partSize);
            if (file.Length % partSize > 0)
                TotalParts += 1;
        }

        public PostedFileInfo PostYEncFile(InntpMessagePoster poster, String comment1, String comment2)
        {
            PostedFileInfo postedFileInfo = new PostedFileInfo();
            String subjectNameBase = ConstructSubjectNameBase(comment1, comment2);
            if (partSize > 1)
                postedFileInfo.NzbSubjectName = String.Format(subjectNameBase, 1);
            else
                postedFileInfo.NzbSubjectName = subjectNameBase;
            postedFileInfo.PostedGroups.AddRange(configuration.TargetNewsgroups);
            postedFileInfo.PostedDateTime = new NntpDateTime(DateTime.Now);

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

        private void PostPart(InntpMessagePoster poster, PostedFileInfo postedFileInfo, YEncFilePart part, String subjectNameBase)
        {
            String subject;
            
            if(TotalParts > 1)
                subject = String.Format(subjectNameBase, part.Number);
            else
                subject = subjectNameBase;

            List<String> yEncPrefix = new List<String>();
            List<String> yEncSuffix = new List<String>();

            if (TotalParts > 1)
            {
                yEncPrefix.Add(String.Format("=ybegin part={0} total={1} line={2} size={3} name={4}",
                    part.Number, TotalParts, configuration.YEncLineSize, file.Length, FileName));

                yEncPrefix.Add(String.Format("=ypart begin={0} end={1}",
                    part.Begin, part.End));

                yEncSuffix.Add(String.Format("=yend size={0} part={1} pcrc32={2}",
                    part.Size, part.Number, part.CRC32));

            }
            else
            {
                yEncPrefix.Add(String.Format("=ybegin line={0} size={1} name={2}",
                    configuration.YEncLineSize, file.Length, FileName));

                yEncSuffix.Add(String.Format("=yend size={0} crc32={1}",
                    file.Length, part.CRC32));
            }

            poster.PostMessage(subject, yEncPrefix, part, yEncSuffix, postedFileInfo);
        }
    }
}
