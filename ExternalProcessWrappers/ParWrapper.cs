using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalProcessWrappers
{
    public class ParWrapper : ExternalProcessWrapperBase
    {
        private Boolean blockSizeTooSmall;
        protected override string ProcessPathLocation
        {
            get { return "par2"; }
        }

        public ParWrapper(Int32 inactiveProcessTimeout)
            : base(inactiveProcessTimeout)
        {
        }

        public String CommandFormat { get; set; }

        public ParWrapper(Int32 inactiveProcessTimeout, String parLocation, String commandFormat) : base(inactiveProcessTimeout, parLocation)
        {
            CommandFormat = commandFormat;
        }

        public void CreateParFilesInDirectory(DirectoryInfo workingFolder, String nameWithoutExtension, Int32 blockSize, Int32 redundancyPercentage, String extraParams)
        {
            blockSizeTooSmall = false;

            String parParameters = String.Format(CommandFormat,
               blockSize,
               redundancyPercentage,
               Path.Combine(workingFolder.FullName, nameWithoutExtension + ".par2"),
               GetFileList(workingFolder),
               extraParams
            );

            try
            {
                this.ExecuteProcess(parParameters);
            }
            catch(Exception ex)
            {
                if (blockSizeTooSmall)
                    throw new Par2BlockSizeTooSmallException("Block size too small", ex);
                throw;
            }
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

        protected override void Process_ErrorDataReceived(object sender, String outputLine)
        {
            base.Process_ErrorDataReceived(sender, outputLine);
            if (outputLine != null && outputLine.Contains("Block size is too small."))
                blockSizeTooSmall = true;
        }
    }
}
