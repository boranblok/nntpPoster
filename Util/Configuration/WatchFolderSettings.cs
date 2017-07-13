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
        public String CharsToRemove { get; set; }
        public String PreTag { get; set; }
        public String PostTag { get; set; }
        internal List<String> TargetNewsgroups { get; set; }
        public Boolean SpreadFilesOverTargetNewsgroups { get; set; }
        public Boolean StripFileMetadata { get; set; }
        public String FromAddress { get; set; }
        public Boolean ApplyRandomPassword { get; set; }
        public String RarPassword { get; set; }
        public Int32 Priority { get; set; }


        private String previousNewsGroup;
        public List<String> GetTargetNewsGroups()
        {
            if (!SpreadFilesOverTargetNewsgroups)
                return TargetNewsgroups;

            String targetNewsGroup;
            
            if(String.IsNullOrWhiteSpace(previousNewsGroup))
            {
                targetNewsGroup = TargetNewsgroups[0];
            }
            else
            {
                var currentIndex = TargetNewsgroups.IndexOf(previousNewsGroup);
                if (currentIndex == TargetNewsgroups.Count - 1)
                    targetNewsGroup = TargetNewsgroups[0];
                else
                    targetNewsGroup = TargetNewsgroups[currentIndex + 1];
            }

            previousNewsGroup = targetNewsGroup;

            return new List<String>(new String[] { targetNewsGroup });
        }
    }
}
