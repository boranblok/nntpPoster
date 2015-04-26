using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nntpAutoposter
{
    //TODO: this entire thing could be more context dependent instead of from appconfig.
    public class AutoPosterConfig
    {
        public String PostTag { get; set; }

        public DirectoryInfo WatchFolder { get; set; }        
        public DirectoryInfo BackupFolder { get; set; }
        public Boolean RemoveAfterVerify { get; set; }

        public Boolean UseHashing { get; set; }
        public String HashedNotificationUrl { get; set; }

        public AutoPosterConfig()
        {
            PostTag = ConfigurationManager.AppSettings["PostTag"];

            WatchFolder = new DirectoryInfo(ConfigurationManager.AppSettings["WatchFolder"]);
            if (!WatchFolder.Exists)
                WatchFolder.Create();

            BackupFolder = new DirectoryInfo(ConfigurationManager.AppSettings["BackupFolder"]);
            if (!BackupFolder.Exists)
                BackupFolder.Create();
            RemoveAfterVerify = Boolean.Parse(ConfigurationManager.AppSettings["RemoveAfterVerify"]);

            UseHashing = Boolean.Parse(ConfigurationManager.AppSettings["UseHashing"]);
            HashedNotificationUrl = ConfigurationManager.AppSettings["HashedNotificationUrl"];
        }
    }
}
