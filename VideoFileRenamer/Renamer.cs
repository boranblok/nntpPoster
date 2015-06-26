using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using log4net;

namespace VideoFileRenamer
{
    class Renamer
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private RenamerConfiguration configuration;


        public Renamer(RenamerConfiguration configuration)
        {
            this.configuration = configuration;
        }       
    }
}
