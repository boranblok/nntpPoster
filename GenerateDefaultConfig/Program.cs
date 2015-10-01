using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Util.Configuration;

namespace GenerateDefaultConfig
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = new Settings();

            settings.NewsGroupAddress = "Address of news host";
            settings.NewsGroupPort = 443;
            settings.NewsGroupUsername = "username";
            settings.NewsGroupPassword = "password";
            settings.NewsGroupUseSsl = true;
            settings.ObfuscatedNotificationUrl = "https://apiserver/notify?apikey=etc";
            settings.SearchUrl = "https://apisever/search?apikey=etc";

            settings.MaxConnectionCount = 10;
            settings.WorkingFolderString = "working";
            settings.NzbOutputFolderString = "";
            settings.BackupFolderString = "backup";
            settings.RarLocation = "";
            settings.ParLocation = "";
            settings.MkvPropEditLocation = "";
            settings.FFmpegLocation = "";
            settings.RemoveAfterVerify = true;
            settings.FilesystemCheckIntervalMillis = 5000;
            settings.FilesystemCheckTesholdMinutes = 5;
            settings.AutoposterIntervalMillis = 5000;
            settings.NotifierIntervalMinutes = 5;
            settings.VerifierIntervalMinutes = 15;
            settings.VerifySimilarityPercentageTreshold = 95;
            settings.RepostAfterMinutes = 240;
            settings.MaxRetryCount = 3;
            settings.InactiveProcessTimeout = 5;
            settings.yEncLineSize = 128;
            settings.yEncLinesPerMessage = 6000;
            settings.DatabaseFile = "";


            settings.RarNParSettings.Add(new RarNParSetting { FromSize = 0, RarSize = 15, Par2Percentage = 10 });
            settings.RarNParSettings.Add(new RarNParSetting { FromSize = 1024, RarSize = 50, Par2Percentage = 10 });
            settings.RarNParSettings.Add(new RarNParSetting { FromSize = 5120, RarSize = 1000, Par2Percentage = 5 });

            var watchfolder = new WatchFolderSettings
            {
                ShortName = "Default",
                PathString = "watch",
                StripFileMetadata = false,
                UseObfuscation = false,
                FromAddress = "bob@bobbers.bob",
                PostTag = ""
            };
            watchfolder.TargetNewsgroups.Add("alt.binaries.multimedia");
            settings.WatchFolderSettings.Add(watchfolder);
            settings.SaveSettings();
        }
    }
}
