using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalProcessWrappers
{
    public class MkvPropEditWrapper
    {
        private String _mkvPropEditLocation = "mkvpropedit";

        public String MkvPropEditLocation
        {
            get { return _mkvPropEditLocation; }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                    _mkvPropEditLocation = "mkvpropedit";   //Assume mkvpropedit is accessible via the PATH environment variable.
                else
                    _mkvPropEditLocation = value;
            }
        }

        public MkvPropEditWrapper()
        {
        }

        public MkvPropEditWrapper(String mkvPropEditLocation)
        {
            MkvPropEditLocation = mkvPropEditLocation;
        }

        public void SetTitle(FileInfo mkvFile, String title)
        {
            String mkvPropEditParameters = String.Format("--set title=\"{0}\" \"{1}\"",
                title,
                mkvFile.FullName
            );

            Process mkvPropEditProcess = new Process();
            mkvPropEditProcess.StartInfo.Arguments = mkvPropEditParameters;
            mkvPropEditProcess.StartInfo.FileName = MkvPropEditLocation;

            mkvPropEditProcess.StartInfo.UseShellExecute = false;
            mkvPropEditProcess.StartInfo.RedirectStandardOutput = true;
            mkvPropEditProcess.StartInfo.RedirectStandardError = true;
            mkvPropEditProcess.StartInfo.CreateNoWindow = true;
            mkvPropEditProcess.ErrorDataReceived += mkvPropEditProcess_ErrorDataReceived;
            mkvPropEditProcess.OutputDataReceived += mkvPropEditProcess_OutputDataReceived;
            mkvPropEditProcess.EnableRaisingEvents = true;
            mkvPropEditProcess.Start();
            mkvPropEditProcess.BeginOutputReadLine();
            mkvPropEditProcess.BeginErrorReadLine();
            mkvPropEditProcess.WaitForExit();
        }

        private void mkvPropEditProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Out.WriteLine(e.Data);
        }

        private void mkvPropEditProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Error.WriteLine(e.Data);
        }
    }
}
