using System;

namespace nntpPoster.yEncLib
{
    public class YEncFilePart
    {
        public String SourcefileName { get; set; }

        public Byte[] EncodedLines { get; set; }

        public String CRC32 { get; set; }

        public Int32 Number { get; set; }

        public Int64 Begin { get; set; }
        public Int64 End { get; set; }

        public Int64 Size
        {
            get
            {
                return End - Begin + 1;
            }
        }
    }
}
