using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nntpPoster
{
    public static class Extensions
    {
        public static Int64 Size(this DirectoryInfo d)
        {
            Int64 Size = 0;   
            foreach (FileInfo fi in d.GetFiles()) 
            {      
                Size += fi.Length;    
            }

            foreach (DirectoryInfo di in d.GetDirectories()) 
            {
                Size += di.Size(); 
            }

            return(Size);  
        }

        public static String NameWithoutExtension(this FileInfo f)
        {
            Int32 extensionPosition = f.Name.LastIndexOf(f.Extension);
            if (extensionPosition > 0)
                return f.Name.Substring(0, extensionPosition - 1);
            return f.Name;
        }
    }
}
