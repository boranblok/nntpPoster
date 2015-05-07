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

        public FFmpegWrapper(Int32 inactiveProcessTimeout)
            : base(inactiveProcessTimeout)
        {
        }

        public FFmpegWrapper(Int32 inactiveProcessTimeout, String ffmpegLocation) : base(inactiveProcessTimeout, ffmpegLocation)
        {
        }

        public void TryStripMetadata(FileInfo mediaFile)
        {
            String originalFile = mediaFile.FullName;
            String tmpFile = originalFile + ".tmp";
            try
            {
                File.Move(originalFile, tmpFile);
                try
                {
                    String ffmpegParameters = String.Format("-i \"{0}\" -map_metadata -1 -vcodec copy -acodec copy \"{1}\"",
                      tmpFile,
                      originalFile
                  );

                    this.ExecuteProcess(ffmpegParameters);
                }
                catch (Exception)
                {
                    if (File.Exists(originalFile))
                    {
                        File.Delete(originalFile);
                    }
                    File.Move(tmpFile, originalFile);
                    throw;
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
    }
}
