using System;
using System.Collections.Generic;
namespace nntpPoster
{
    public interface InntpMessagePoster
    {
        void PostMessage(String subject, List<String> prefix, Byte[] yEncBody, List<String> suffix);
    }
}
