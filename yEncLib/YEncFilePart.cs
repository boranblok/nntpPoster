using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nntpPoster.yEncLib
{
    public class YEncFilePart
    {
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
