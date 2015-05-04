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

        protected DateTime LastOutputReceivedAt { get; set; }

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

        protected ExternalProcessWrapperBase()
        {
            ProcessLocation = ProcessPathLocation;
        }

        protected ExternalProcessWrapperBase(String processLocation)
        {
            ProcessLocation = processLocation;
        }

        protected void ExecuteProcess(String parameters)
        {
            using (var process = new Process())
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

                while (!process.WaitForExit(60*1000))
                {
                    if ((DateTime.Now - LastOutputReceivedAt).TotalMinutes > 5)     //TODO: make a parameter to give slower systems more timeout ?
                    {
                        Console.WriteLine("No output received for 5 minutes, killing external process.");
                        process.Kill();
                        throw new Exception("External process had to be killed due to inactivity.");
                    }
                }

                if (process.ExitCode != 0)
                {
                    throw new Exception("Process has exited with errors. Exit code: " + process.ExitCode);
                }
            }
        }

        protected virtual void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            LastOutputReceivedAt = DateTime.Now;
            Console.Out.WriteLine(DateTime.Now.ToString("HH:mm:ss,fff") + " " + e.Data);
        }

        protected virtual void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            LastOutputReceivedAt = DateTime.Now;
            Console.Error.WriteLine(DateTime.Now.ToString("HH:mm:ss,fff") + " " + e.Data);
        }
    }
}
