using System;
using System.Collections.Generic;
using System.IO;

namespace Util.Configuration
{
    public class WatchFolderSettings
    {
        public WatchFolderSettings()
        {
            TargetNewsgroups = new List<string>();
        }

        public String ShortName { get; set; }
        public DirectoryInfo Path{ get; set; }
        public Boolean UseObfuscation { get; set; }
        public Boolean CleanName { get; set; }
        public String PreTag { get; set; }
        public String PostTag { get; set; }
        public List<String> TargetNewsgroups { get; set; }
        public Boolean StripFileMetadata { get; set; }
        public String FromAddress { get; set; }
        public Boolean ApplyRandomPassword { get; set; }
        public String RarPassword { get; set; }
        public Int32 Priority { get; set; }
    }
}
