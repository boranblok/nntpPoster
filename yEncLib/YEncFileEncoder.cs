using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nntpPoster.yEncLib
{
    public class YEncFileEncoder
    {
        public Int32 LineLength { get; set; }
        public Int32 MaxLinesPerMessage { get; set; }

        public YEncFileEncoder()
        {
            //Default values for yEnc.
            LineLength = 128;
            MaxLinesPerMessage = 3000;
        }

        public YEncEncodedFile EncodeFile(FileInfo fileToEncode)
        {            
            var encodedFile = new YEncEncodedFile(LineLength, fileToEncode);
            var yEncoder = new YEncEncoder();
            
            using(var fileStream = fileToEncode.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                encodedFile.FileSize = fileStream.Length;

                var readBuffer = new Byte[LineLength * MaxLinesPerMessage];
                Int32 bytesRead;
                
                var part = new YEncFilePart();
                part.Begin = fileStream.Position;
                CRC32 partCRCCalculator = new CRC32();
                CRC32 fileCRCCalculator = new CRC32();
                Int32 lineCount = 0;
                while((bytesRead = fileStream.Read(readBuffer, 0, LineLength)) > 0)
                {
                    byte[] encodedLine = yEncoder.EncodeLine(readBuffer, 0, bytesRead);
                    //part.AddEncodedLine(encodedLine);
                    if (++lineCount < MaxLinesPerMessage && fileStream.Position < fileStream.Length - 1)
                    {
                        partCRCCalculator.TransformBlock(readBuffer, 0, bytesRead, readBuffer, 0);
                        fileCRCCalculator.TransformBlock(readBuffer, 0, bytesRead, readBuffer, 0);
                    }
                    else
                    {
                        partCRCCalculator.TransformFinalBlock(readBuffer, 0, bytesRead);
                        part.CRC32 = partCRCCalculator.HashAsHexString;
                        part.End = fileStream.Position;
                        encodedFile.Parts.Add(part);

                        if (fileStream.Position < fileStream.Length - 1)
                        {
                            fileCRCCalculator.TransformBlock(readBuffer, 0, bytesRead, readBuffer, 0);
                            part = new YEncFilePart();
                            part.Begin = fileStream.Position;   //TODO: either end or start of these needs an offset by 1, which one ?
                            partCRCCalculator = new CRC32();
                            lineCount = 0;
                        }
                        else
                        {
                            fileCRCCalculator.TransformFinalBlock(readBuffer, 0, bytesRead);
                            encodedFile.FileCRC32 = fileCRCCalculator.HashAsHexString;
                        }
                    }
                }
            }

            return null;
        }
    }
}
