using System;
using System.Collections.Generic;
using nntpPoster.yEncLib;

namespace nntpPoster
{
    public interface INntpMessagePoster
    {
        void PostMessage(String subject, List<String> prefix, YEncFilePart yEncPart, List<String> suffix,
            PostedFileInfo postInfo);
    }
}
