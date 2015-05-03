using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalProcessWrappers
{
    public abstract class ExternalProcessWrapperBase
    {
        protected abstract String ProcessPathLocation { get; }

        private String _processLocation;

        public String ProcessLocation
        {
            get { return _processLocation; }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                    _processLocation = ProcessPathLocation;   //Assume the process is accessible via the PATH environment variable.
                else
                    _processLocation = value;
            }
        }

        public ExternalProcessWrapperBase()
        {
            ProcessLocation = ProcessPathLocation;
        }

        public ExternalProcessWrapperBase(String processLocation)
        {
            ProcessLocation = processLocation;
        }

        protected void ExecuteProcess(String parameters)
        {
            using (Process process = new Process())
            {
                process.StartInfo.Arguments = parameters;
                process.StartInfo.FileName = ProcessLocation;

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.OutputDataReceived += Process_OutputDataReceived;
                process.EnableRaisingEvents = true;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
        }

        protected virtual void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Out.WriteLine(e.Data);
        }

        protected virtual void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Error.WriteLine(e.Data);
        }
    }
}
