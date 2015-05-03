using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalProcessWrappers
{
    public class FFmpegWrapper : ExternalProcessWrapperBase
    {
        protected override string ProcessPathLocation
        {
            get { return "ffmpeg"; }
        }

        private Boolean haserror;

        public FFmpegWrapper() : base()
        {
        }

        public FFmpegWrapper(String ffmpegLocation) : base(ffmpegLocation)
        {
        }

        public void TryStripMetadata(FileInfo mediaFile)
        {
            haserror = false;
            String originalFile = mediaFile.FullName;
            String tmpFile = originalFile + ".tmp";
            try
            {
                File.Move(originalFile, tmpFile);
                try
                {
                    //FFMpeg outputs all output to sterr, making error detection very hard, 
                    //therefore we have set verbosity to 16, level 8 might be required if this is still to conservative.
                    String ffmpegParameters = String.Format("-v 16 -i \"{0}\" -map_metadata -1 -vcodec copy -acodec copy \"{1}\"",
                      tmpFile,
                      originalFile
                  );

                    this.ExecuteProcess(ffmpegParameters);
                }
                catch (Exception)
                {
                    haserror = true;
                    throw;
                }
                if (haserror)   // If we had any error we revert back to the original file.
                {
                    if (File.Exists(originalFile))
                    {
                        File.Delete(originalFile);
                    }
                    File.Move(tmpFile, originalFile);
                }
            }
            finally //The tempfile should always be removed so we dont leave stuff behind.
            {
                if (File.Exists(tmpFile))
                {
                    File.Delete(tmpFile);
                }
            }
            mediaFile.Refresh();
        }

        protected override void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(e.Data)) //FFmpeg outputs null lines to stderr.
            {
                haserror = true;
                Console.Out.WriteLine(e.Data);
            }
        }
    }
}
