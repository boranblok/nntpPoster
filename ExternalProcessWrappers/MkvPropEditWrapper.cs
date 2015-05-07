using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalProcessWrappers
{
    public class MkvPropEditWrapper : ExternalProcessWrapperBase
    {
        protected override string ProcessPathLocation
        {
            get { return "mkvpropedit"; }
        }

        public MkvPropEditWrapper(Int32 inactiveProcessTimeout)
            : base(inactiveProcessTimeout)
        {
        }

        public MkvPropEditWrapper(Int32 inactiveProcessTimeout, String mkvPropEditLocation) : base(inactiveProcessTimeout, mkvPropEditLocation)
        {
        }

        public void SetTitle(FileInfo mkvFile, String title)
        {
            String mkvPropEditParameters = String.Format("--set title=\"{0}\" \"{1}\"",
                title,
                mkvFile.FullName
            );

            this.ExecuteProcess(mkvPropEditParameters);
        }
    }
}
