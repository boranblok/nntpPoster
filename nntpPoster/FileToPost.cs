using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using nntpPoster.yEncLib;

namespace nntpPoster
{
    public class FileToPost
    {
        private Int32 partSize;
        private FileInfo file;

        public String FileName { get; private set; }
        public Int32 TotalParts { get; private set; }



        public FileToPost(FileInfo fileToPost)
        {
            partSize = PostSettings.YEncLineSize * PostSettings.YEncLinesPerMessage;

            file = fileToPost;
            DetermineFileName();
            DetermineTotalParts();
        }

        private void DetermineFileName()
        {
            FileName = Regex.Replace(file.Name, "^[:ascii:]", String.Empty);
            FileName = FileName.Replace(' ', '.');
        }

        private void DetermineTotalParts()
        {
            TotalParts = (Int32)(file.Length / partSize);
            if (file.Length % partSize > 0)
                TotalParts += 1;
        }

        public void PostYEncFile(InntpMessagePoster poster, String comment1, String comment2)
        {
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
                    part.EncodedLines = yEncoder.EncodeBlock(PostSettings.YEncLineSize, partBuffer, 0, partBufferPos);

                    partCRCCalculator.TransformFinalBlock(partBuffer, 0, partBufferPos);
                    part.CRC32 = partCRCCalculator.HashAsHexString;

                    part.End = fileStream.Position;

                    PostPart(poster, part, comment1, comment2);
                }
            }
        }

        private void PostPart(InntpMessagePoster poster, YEncFilePart part, String comment1, String comment2)
        {
            StringBuilder subject = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(comment1))
                subject.AppendFormat("[{0}] ", comment1);
            subject.AppendFormat("\"" + FileName + "\"");
            subject.Append(" yEnc ");
            if (TotalParts > 1)
                subject.AppendFormat("({0}/{1}) ", part.Number, TotalParts);
            subject.Append(file.Length);
            if (!String.IsNullOrWhiteSpace(comment2))
                subject.AppendFormat(" [{0}]", comment2);

            List<String> yEncPrefix = new List<String>();
            List<String> yEncSuffix = new List<String>();

            if (TotalParts > 1)
            {
                yEncPrefix.Add(String.Format("=ybegin part={0} total={1} line={2} size={3} name={4}",
                    part.Number, TotalParts, PostSettings.YEncLineSize, file.Length, FileName));

                yEncPrefix.Add(String.Format("=ypart begin={0} end={1}",
                    part.Begin, part.End));

                yEncSuffix.Add(String.Format("=yend size={0} part={1} pcrc32={2}",
                    part.Size, part.Number, part.CRC32));

            }
            else
            {
                yEncPrefix.Add(String.Format("=ybegin line={0} size={1} name={2}",
                    PostSettings.YEncLineSize, file.Length, FileName));

                yEncSuffix.Add(String.Format("=yend size={0} crc32={1}",
                    file.Length, part.CRC32));
            }

            poster.PostMessage(subject.ToString(), yEncPrefix, part.EncodedLines, yEncSuffix);
        }
    }
}
