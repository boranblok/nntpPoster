using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostingNntpClient
{
    internal class NntpResponse
    {
        public Int32 ResponseCode { get; private set; }
        public String ResponseMessage { get; set; }

        public NntpResponse(String responseMessage)
        {
            ResponseMessage = responseMessage;
            ResponseCode = ExtractResponseCode(responseMessage);
        }

        private Int32 ExtractResponseCode(String responseMessage)
        {
            Int32 code;
            if(!String.IsNullOrWhiteSpace(responseMessage) && Int32.TryParse(responseMessage.Substring(0,3), out code))
            {
                return code;
            }

            return 0;
        }
    }
}
