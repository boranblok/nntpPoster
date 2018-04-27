﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nntpPoster.yEncLib;

namespace nntpPoster
{
    public class NntpMessage
    {
        public String FromAddress { get; set; }
        public String Subject { get; set; }
        public List<String> Prefix { get; set; }
        public YEncFilePart YEncFilePart { get; set; }
        public List<String> Suffix { get; set; }
        public PostedFileInfo PostInfo { get; set; }

        public NntpMessage()
        {
            Prefix = new List<String>();
            Suffix = new List<String>();
        }
    }
}
