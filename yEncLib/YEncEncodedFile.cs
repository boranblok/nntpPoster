using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nntpPoster.yEncLib
{
    public class YEncEncodedFile
    {
        public FileInfo File { get; private set; }
        public Int32 LineLength { get; private set; }
        public List<YEncFilePart> Parts { get; private set; }

        public String FileCRC32 { get; set; }
        public Int64 FileSize { get; set; }

        internal YEncEncodedFile(Int32 lineLength, FileInfo file)
        {
            this.File = file;
            this.LineLength = lineLength;
            Parts = new List<YEncFilePart>();
        }
    }
}
