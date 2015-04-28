using System;
using System.Diagnostics;
using System.IO;

namespace rarLib
{
    public class RarWrapper
    {
        private String _rarLocation = "rar";

        public String RarLocation
        {
            get { return _rarLocation; }
            set 
            {
                if (String.IsNullOrWhiteSpace(value))
                    _rarLocation = "rar";   //Assume rar is accessible via the PATH environment variable.
                else
                    _rarLocation = value; 
            }
        }

        public RarWrapper()
        {
        }

        public RarWrapper(String rarLocation)
        {
            RarLocation = rarLocation;
        }

        public void Compress(FileSystemInfo source, DirectoryInfo destination, String archiveName, Int32 partSize)
        {
            String toCompress;
            var attributes = File.GetAttributes(source.FullName);
            if (attributes.HasFlag(FileAttributes.Directory))
                toCompress = Path.Combine(source.FullName, "*");
            else
                toCompress = source.FullName;

            var rarParameters = String.Format("a -ep1 -m0 -v{0}b \"{1}\" \"{2}\"",
                partSize,
                Path.Combine(destination.FullName, archiveName),
                toCompress
            );

            var rarProcess = new Process();
            rarProcess.StartInfo.Arguments = rarParameters;
            rarProcess.StartInfo.FileName = RarLocation;

            rarProcess.StartInfo.UseShellExecute = false;
            rarProcess.StartInfo.RedirectStandardOutput = true;
            rarProcess.StartInfo.RedirectStandardError = true;
            rarProcess.StartInfo.CreateNoWindow = true;
            rarProcess.ErrorDataReceived += rarProcess_ErrorDataReceived;
            rarProcess.OutputDataReceived += rarProcess_OutputDataReceived;
            rarProcess.EnableRaisingEvents = true;
            rarProcess.Start();
            rarProcess.BeginOutputReadLine();
            rarProcess.BeginErrorReadLine();
            rarProcess.WaitForExit();
        }

        private void rarProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Out.WriteLine(e.Data);
        }

        private void rarProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Error.WriteLine(e.Data);
        }
    }
}
