using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ExternalProcessWrappers
{
    public abstract class ExternalProcessWrapperBase
    {
        protected static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

        public Int32 InactiveProcessTimeout { get; set; }

        private StringBuilder outDataTmp;
        private StringBuilder errDataTmp;
        private StdStreamReader stdoutReader;
        private StdStreamReader stderrReader;

        protected ExternalProcessWrapperBase(Int32 inactiveProcessTimeout)
        {
            InactiveProcessTimeout = inactiveProcessTimeout;
            ProcessLocation = ProcessPathLocation;
        }

        protected ExternalProcessWrapperBase(Int32 inactiveProcessTimeout, String processLocation)
        {
            InactiveProcessTimeout = inactiveProcessTimeout;
            ProcessLocation = processLocation;
        }

        protected void ExecuteProcess(String parameters)
        {
            outDataTmp = new StringBuilder();
            errDataTmp = new StringBuilder();
            stdoutReader = new StdStreamReader();
            stderrReader = new StdStreamReader();

            stdoutReader.DataReceivedEvent +=
                new EventHandler<DataReceived>(stdoutReader_DataReceivedEvent);
            stderrReader.DataReceivedEvent +=
                new EventHandler<DataReceived>(stderrReeader_DataReceivedEvent);

            using (var process = new Process())
            {
                process.StartInfo.Arguments = parameters;
                process.StartInfo.FileName = ProcessLocation;

                log.DebugFormat("Executing process: [{0} {1}]", ProcessLocation, parameters);

                LastOutputReceivedAt = DateTime.Now;

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                stdoutReader.StartReader(process.StandardOutput.BaseStream, process);
                stderrReader.StartReader(process.StandardError.BaseStream, process);


                while (!process.WaitForExit(60*1000))
                {
                    process.Refresh();
                    if (process.HasExited)
                    {
                        stdoutReader.IsDone();
                        stderrReader.IsDone();
                        break;
                    }

                    if ((DateTime.Now - LastOutputReceivedAt).TotalMinutes > InactiveProcessTimeout)
                    {
                        log.WarnFormat("No output received for {0} minutes, killing external process.", InactiveProcessTimeout);
                        stdoutReader.IsDone();
                        stderrReader.IsDone();
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

        private void stdoutReader_DataReceivedEvent(object sender, DataReceived e)
        {
            LastOutputReceivedAt = DateTime.Now;
            outDataTmp.Append(e.Data);
            //log.DebugFormat("std output contains [{0}]", outDataTmp);
            Int32 indexOfNewLine = outDataTmp.ToString().IndexOf(Environment.NewLine);
            //log.DebugFormat("newline found at [{0}]", indexOfNewLine);
            while (indexOfNewLine >= 0)
            {
                String outputLine = outDataTmp.ToString().Substring(0, indexOfNewLine);
                //log.DebugFormat("std output line [{0}]", outputLine);

                Process_OutputDataReceived(sender, outputLine);

                //log.Debug("removing");
                outDataTmp.Remove(0, indexOfNewLine + Environment.NewLine.Length);
                //log.DebugFormat("std output contains [{0}]", outDataTmp);
                indexOfNewLine = outDataTmp.ToString().IndexOf(Environment.NewLine);
                //log.DebugFormat("newline found at [{0}]", indexOfNewLine);
            }
            //log.DebugFormat("std output contains [{0}]", outDataTmp);
        }

        private void stderrReeader_DataReceivedEvent(object sender, DataReceived e)
        {
            LastOutputReceivedAt = DateTime.Now;
            errDataTmp.Append(e.Data);

            //log.DebugFormat("err output contains [{0}]", errDataTmp);
            Int32 indexOfNewLine = errDataTmp.ToString().IndexOf(Environment.NewLine);
            //log.DebugFormat("newline found at [{0}]", indexOfNewLine);
            while (indexOfNewLine >= 0)
            {
                String errLine = errDataTmp.ToString().Substring(0, indexOfNewLine);
                //log.DebugFormat("err output line [{0}]", errLine);

                Process_ErrorDataReceived(sender, errLine);

                //log.Debug("removing");
                errDataTmp.Remove(0, indexOfNewLine + Environment.NewLine.Length);
                //log.DebugFormat("err output contains [{0}]", errDataTmp);
                indexOfNewLine = errDataTmp.ToString().IndexOf(Environment.NewLine);
                //log.DebugFormat("newline found at [{0}]", indexOfNewLine);
            }
            //log.DebugFormat("err output contains [{0}]", errDataTmp);
        }

        protected virtual void Process_OutputDataReceived(object sender, String outputLine)
        {            
            if(!String.IsNullOrWhiteSpace(outputLine))
                log.Debug(outputLine);
        }

        protected virtual void Process_ErrorDataReceived(object sender, String outputLine)
        {
            if (!String.IsNullOrWhiteSpace(outputLine))
                log.Warn(outputLine);
        }
    }
}
