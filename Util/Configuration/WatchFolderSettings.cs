using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Util.Configuration
{
    [DataContract(Namespace = "Util.Configuration")]
    public class WatchFolderSettings
    {
        public WatchFolderSettings()
        {
            TargetNewsgroups = new List<string>();
        }

        [DataMember]
        public String ShortName { get; set; }

        [DataMember(Name = "Path")]
        public String PathString { get; set; }
        public DirectoryInfo Path
        {
            get { return Settings.GetOrCreateFolder(PathString); }
            set { PathString = value.FullName; }
        }


        [DataMember]
        public Boolean UseObfuscation { get; set; }

        [DataMember]
        public Boolean CleanName { get; set; }

        [DataMember]
        public String PostTag { get; set; }

        [DataMember]
        public List<String> TargetNewsgroups { get; set; }

        [DataMember]
        public Boolean StripFileMetadata { get; set; }

        [DataMember]
        public String FromAddress { get; set; }

        [DataMember]
        public Boolean ApplyRandomPassword { get; set; }

        [DataMember]
        public String RarPassword { get; set; }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            //This is redundant, but added for clarity.
            ApplyRandomPassword = false;
            RarPassword = null;
        }
    }
}
