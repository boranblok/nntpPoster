using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace par2Lib
{
    public class ParWrapper
    {
        private String _parLocation = "par2";

        public String ParLocation
        {
            get { return _parLocation; }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                    _parLocation = "par2";   //Assume par2 is accessible via the PATH environment variable.
                else
                    _parLocation = value;
            }
        }

        public ParWrapper()
        {
        }

        public ParWrapper(String parLocation)
        {
            ParLocation = parLocation;
        }

        public void CreateParFilesInDirectory(DirectoryInfo workingFolder, String nameWithoutExtension, Int32 blockSize, Int32 redundancyPercentage)
        {
            String parParameters = String.Format("c -s{0} -r{1} -- \"{2}\"{3}",
               blockSize,
               redundancyPercentage,
               Path.Combine(workingFolder.FullName, nameWithoutExtension + ".par2"),
               GetFileList(workingFolder)
            );

            Console.WriteLine("par2 {0}", parParameters);

            Process parProcess = new Process();
            parProcess.StartInfo.Arguments = parParameters;
            parProcess.StartInfo.FileName = ParLocation;

            parProcess.StartInfo.UseShellExecute = false;
            parProcess.StartInfo.RedirectStandardOutput = true;
            parProcess.StartInfo.RedirectStandardError = true;
            parProcess.StartInfo.CreateNoWindow = true;
            parProcess.ErrorDataReceived += parProcess_ErrorDataReceived;
            parProcess.OutputDataReceived += parProcess_OutputDataReceived;
            parProcess.EnableRaisingEvents = true;
            parProcess.Start();
            parProcess.BeginOutputReadLine();
            parProcess.BeginErrorReadLine();
            parProcess.WaitForExit();
        }

        private String GetFileList(DirectoryInfo workingFolder)
        {
            var allFiles = workingFolder.GetFileSystemInfos("*", SearchOption.AllDirectories);
            StringBuilder fileList = new StringBuilder();
            foreach (var file in allFiles)
            {
                fileList.Append(" \"");
                fileList.Append(file.FullName);
                fileList.Append("\"");
            }

            return fileList.ToString();
        }

        private void parProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Out.WriteLine(e.Data);
        }

        private void parProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Error.WriteLine(e.Data);
        }
    }
}
