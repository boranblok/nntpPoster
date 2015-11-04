using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Util.Configuration
{
    [DataContract(Namespace = "Util.Configuration")]
    public class RarNParSetting
    {
        [DataMember]
        public Int32 FromSize { get; set; }

        public Int64 FromSizeBytes
        {
            get { return FromSize*1024*1024; }
        }

        [DataMember]
        public Int32 RarSize { get; set; }

        [DataMember]
        public Int32 Par2Percentage { get; set; }
    }
}
