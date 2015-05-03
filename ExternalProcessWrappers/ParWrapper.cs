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
        protected override string ProcessPathLocation
        {
            get { return "par2"; }
        }

        public ParWrapper() : base()
        {
        }

        public ParWrapper(String parLocation) :base(parLocation)
        {
        }

        public void CreateParFilesInDirectory(DirectoryInfo workingFolder, String nameWithoutExtension, Int32 blockSize, Int32 redundancyPercentage)
        {
            String parParameters = String.Format("c -q -s{0} -r{1} -- \"{2}\"{3}",
               blockSize,
               redundancyPercentage,
               Path.Combine(workingFolder.FullName, nameWithoutExtension + ".par2"),
               GetFileList(workingFolder)
            );

            this.ExecuteProcess(parParameters);
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
    }
}
