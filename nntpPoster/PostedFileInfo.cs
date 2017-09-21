using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nntpPoster
{
    public class PostedFileInfo
    {
        public String FromAddress { get; set; }
        public String NzbSubjectName { get; set; }
        public DateTime PostedDateTime { get; set; }
        public List<String> PostedGroups { get; private set; }
        public List<PostedFileSegment> Segments { get; private set; }

        public PostedFileInfo()
        {
            PostedGroups = new List<String>();
            Segments = new List<PostedFileSegment>();
        }

        internal Int32 GetUnixPostedDateTime()
        {
            return (Int32)(PostedDateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
