using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nntpPoster
{
    public class PostedFileInfo
    {
        public String NzbSubjectName { get; set; }
        public List<String> PostedGroups { get; set; }
        public List<PostedFileSegment> Segments { get; set; }
    }
}
