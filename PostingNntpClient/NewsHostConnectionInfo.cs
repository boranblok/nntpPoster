using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostingNntpClient
{
    public class NewsHostConnectionInfo
    {
        public String Address { get; set; }
        public Int32 Port { get; set; }
        public Boolean UseSsl { get; set; }
        public String Username { get; set; }
        public String Password { get; set; }
        public Int32 TcpTimeoutSeconds { get; set; }
    }
}
