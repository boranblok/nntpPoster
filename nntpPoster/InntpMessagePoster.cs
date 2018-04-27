using System;
using System.Collections.Generic;
using nntpPoster.yEncLib;

namespace nntpPoster
{
    public interface InntpMessagePoster
    {
        void PostMessage(NntpMessage message);
    }
}
