using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NntpClientLib;

namespace nntpPoster
{
    public class PostedFileInfo
    {
        public String NzbSubjectName { get; set; }
        public NntpDateTime PostedDateTime { get; set; }
        public List<String> PostedGroups { get; private set; }
        public List<PostedFileSegment> Segments { get; private set; }

        public PostedFileInfo()
        {
            PostedGroups = new List<String>();
            Segments = new List<PostedFileSegment>();
        }
    }
}
