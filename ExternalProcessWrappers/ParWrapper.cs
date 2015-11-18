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

        public ParWrapper(Int32 inactiveProcessTimeout, String parLocation) :base(inactiveProcessTimeout, parLocation)
        {
        }

        public void CreateParFilesInDirectory(DirectoryInfo workingFolder, String nameWithoutExtension, Int32 blockSize, Int32 redundancyPercentage)
        {
            blockSizeTooSmall = false;

            String parParameters = String.Format("c -s{0} -r{1} -- \"{2}\"{3}",
               blockSize,
               redundancyPercentage,
               Path.Combine(workingFolder.FullName, nameWithoutExtension + ".par2"),
               GetFileList(workingFolder)
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

        protected override void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            base.Process_ErrorDataReceived(sender, e);
            if (e.Data != null && e.Data.Contains("Block size is too small."))
                blockSizeTooSmall = true;
        }
    }
}
