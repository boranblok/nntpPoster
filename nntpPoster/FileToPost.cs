using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NntpClientLib;
using nntpPoster.yEncLib;

namespace nntpPoster
{
    public class FileToPost
    {
        private readonly UsenetPosterConfig _configuration;
        private readonly Int32 _partSize;
        private readonly FileInfo _file;

        public Int32 TotalParts { get; private set; }

        public FileToPost(UsenetPosterConfig configuration, FileInfo fileToPost)
        {
            _configuration = configuration;
            _partSize = configuration.YEncPartSize;

            _file = fileToPost;
            DetermineTotalParts();
        }

        private string ConstructSubjectNameBase(String prefix, String suffix)
        {
            var subject = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(prefix))
                subject.Append(prefix + " ");
            subject.AppendFormat("\"" + _file.Name + "\"");
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
            TotalParts = (Int32)(_file.Length / _partSize);
            if (_file.Length % _partSize > 0)
                TotalParts += 1;
        }

        public PostedFileInfo PostYEncFile(INntpMessagePoster poster, String prefix, String suffix)
        {
            var postedFileInfo = new PostedFileInfo();
            var subjectNameBase = ConstructSubjectNameBase(prefix, suffix);
            postedFileInfo.NzbSubjectName = String.Format(subjectNameBase, 1);
            postedFileInfo.PostedGroups.AddRange(_configuration.TargetNewsgroups);
            postedFileInfo.PostedDateTime = new NntpDateTime(DateTime.UtcNow);

            var yEncoder = new YEncEncoder();
            var partNumber = 0;

            using (var fileStream = _file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                while (fileStream.Position < fileStream.Length - 1) //If we have more blocks to encode.
                {
                    partNumber++;
                    var partBuffer = new Byte[_partSize];
                    var partBufferPos = 0;
                    var bytesRead = 0;
                    var partCrcCalculator = new CRC32();

                    var part = new YEncFilePart();
                    part.SourcefileName = _file.Name;
                    part.Number = partNumber;
                    part.Begin = fileStream.Position + 1;

                    while (partBufferPos < _partSize - 1 &&
                        (bytesRead = fileStream.Read(partBuffer, partBufferPos, _partSize - partBufferPos)) > 0)
                    {
                        partBufferPos += bytesRead;
                    }

                    //TODO: this can be split in 2 threads to spread out CPU usage.
                    part.EncodedLines = yEncoder.EncodeBlock(_configuration.YEncLineSize, partBuffer, 0, partBufferPos);

                    partCrcCalculator.TransformFinalBlock(partBuffer, 0, partBufferPos);
                    part.CRC32 = partCrcCalculator.HashAsHexString;

                    part.End = fileStream.Position;

                    PostPart(poster, postedFileInfo, part, subjectNameBase);
                }
            }

            return postedFileInfo;
        }

        private void PostPart(INntpMessagePoster poster, PostedFileInfo postedFileInfo, YEncFilePart part, 
            String subjectNameBase)
        {
            var subject = String.Format(subjectNameBase, part.Number);

            var yEncPrefix = new List<String>();
            var yEncSuffix = new List<String>();

            if (TotalParts > 1)
            {
                yEncPrefix.Add(String.Format("=ybegin part={0} total={1} line={2} size={3} name={4}",
                    part.Number, TotalParts, _configuration.YEncLineSize, _file.Length, _file.Name));

                yEncPrefix.Add(String.Format("=ypart begin={0} end={1}",
                    part.Begin, part.End));

                yEncSuffix.Add(String.Format("=yend size={0} part={1} pcrc32={2}",
                    part.Size, part.Number, part.CRC32));

            }
            else
            {
                yEncPrefix.Add(String.Format("=ybegin line={0} size={1} name={2}",
                    _configuration.YEncLineSize, _file.Length, _file.Name));

                yEncSuffix.Add(String.Format("=yend size={0} crc32={1}",
                    _file.Length, part.CRC32));
            }

            poster.PostMessage(subject, yEncPrefix, part, yEncSuffix, postedFileInfo);
        }
    }
}
