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

        public RarWrapper(Int32 inactiveProcessTimeout)
            : base(inactiveProcessTimeout)
        {
        }

        public RarWrapper(Int32 inactiveProcessTimeout, String rarLocation) : base(inactiveProcessTimeout, rarLocation)
        {
        }

        public void Compress(FileSystemInfo source, DirectoryInfo destination, String archiveName, 
            Int32 partSize, String password, String extraParams)
        {
            String toCompress;
            FileAttributes attributes = File.GetAttributes(source.FullName);
            if (attributes.HasFlag(FileAttributes.Directory))
                toCompress = Path.Combine(source.FullName, "*");
            else
                toCompress = source.FullName;

            String passwordParam = String.Empty;
            if (!String.IsNullOrWhiteSpace(password))
                passwordParam = "-hp\"" + password + "\"";

            String rarParameters = String.Format("a -ep1 {0} -m0 -r -v{1}b \"{2}\" \"{3}\" ",
                passwordParam,
                partSize,
                Path.Combine(destination.FullName, archiveName),
                toCompress,
                extraParams
            );

            this.ExecuteProcess(rarParameters);
        }
    }
}
