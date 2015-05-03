using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalProcessWrappers
{
    public class RarWrapper : ExternalProcessWrapperBase
    {
        protected override string ProcessPathLocation
        {
            get { return "rar"; }
        }

        public RarWrapper() : base()
        {
        }

        public RarWrapper(String rarLocation) : base(rarLocation)
        {
        }

        public void Compress(FileSystemInfo source, DirectoryInfo destination, String archiveName, Int32 partSize)
        {
            String toCompress;
            FileAttributes attributes = File.GetAttributes(source.FullName);
            if (attributes.HasFlag(FileAttributes.Directory))
                toCompress = Path.Combine(source.FullName, "*");
            else
                toCompress = source.FullName;

            String rarParameters = String.Format("a -ep1 -m0 -v{0}b \"{1}\" \"{2}\"",
                partSize,
                Path.Combine(destination.FullName, archiveName),
                toCompress
            );

            this.ExecuteProcess(rarParameters);
        }
    }
}
