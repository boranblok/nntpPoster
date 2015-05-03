using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalProcessWrappers
{
    public class FFmpegWrapper
    {
        private String _ffmpegLocation = "ffmpeg";
        private Boolean haserror;

        public String FFmpegLocation
        {
            get { return _ffmpegLocation; }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                    _ffmpegLocation = "ffmpeg";   //Assume ffmpeg is accessible via the PATH environment variable.
                else
                    _ffmpegLocation = value;
            }
        }

        public FFmpegWrapper()
        {
        }

        public FFmpegWrapper(String ffmpegLocation)
        {
            FFmpegLocation = ffmpegLocation;
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

                    Process ffmpegProcess = new Process();
                    ffmpegProcess.StartInfo.Arguments = ffmpegParameters;
                    ffmpegProcess.StartInfo.FileName = FFmpegLocation;

                    ffmpegProcess.StartInfo.UseShellExecute = false;
                    ffmpegProcess.StartInfo.RedirectStandardOutput = true;
                    ffmpegProcess.StartInfo.RedirectStandardError = true;
                    ffmpegProcess.StartInfo.CreateNoWindow = true;
                    ffmpegProcess.ErrorDataReceived += ffmpegProcess_ErrorDataReceived;
                    ffmpegProcess.OutputDataReceived += ffmpegProcess_OutputDataReceived;
                    ffmpegProcess.EnableRaisingEvents = true;
                    ffmpegProcess.Start();
                    ffmpegProcess.BeginOutputReadLine();
                    ffmpegProcess.BeginErrorReadLine();
                    ffmpegProcess.WaitForExit();
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

        private void ffmpegProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(e.Data)) //FFmpeg outputs null lines to stderr.
            {
                haserror = true;
                Console.Out.WriteLine(e.Data);
            }
        }

        private void ffmpegProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Error.WriteLine(e.Data);
        }
    }
}
