using System;

namespace nntpPoster
{
    public class PostedFileSegment
    {
        public Int64 Bytes { get; set; }
        public Int32 SegmentNumber { get; set; }
        public String MessageId { get; set; }
        public String MessageIdWithoutBrackets 
        { 
            get
            {
                return MessageId.TrimStart('<').TrimEnd('>');
            }
        }
    }
}
