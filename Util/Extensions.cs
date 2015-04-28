using System;
using System.IO;
using System.Linq;

namespace Util
{
    public static class Extensions
    {
        public static Int64 Size(this FileSystemInfo fsi)
        {
            var attributes = File.GetAttributes(fsi.FullName);
            if (attributes.HasFlag(FileAttributes.Directory))
                return new DirectoryInfo(fsi.FullName).Size();
            return new FileInfo(fsi.FullName).Length;
        }

        public static Int64 Size(this DirectoryInfo d)
        {
            var size = d.GetFiles().Sum(fi => fi.Length);
            size += d.GetDirectories().Sum(di => di.Size());

            return (size);
        }

        public static String NameWithoutExtension(this FileSystemInfo fsi)
        {
            var attributes = File.GetAttributes(fsi.FullName);
            if (attributes.HasFlag(FileAttributes.Directory))
                return new DirectoryInfo(fsi.FullName).Name;
            return NameWithoutExtension((FileSystemInfo) new FileInfo(fsi.FullName));
        }

        public static String NameWithoutExtension(this FileInfo f)
        {
            var extensionPosition = f.Name.LastIndexOf(f.Extension, StringComparison.Ordinal);
            if (extensionPosition > 0)
                return f.Name.Substring(0, extensionPosition);
            return f.Name;
        }

        public static void Copy(this DirectoryInfo sourceDirectory, String destDirName, Boolean copySubDirs)
        {
            var dirs = sourceDirectory.GetDirectories();

            if (!sourceDirectory.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirectory.FullName);
            }

            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            var files = sourceDirectory.GetFiles();
            foreach (var file in files)
            {
                var temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (var subdir in dirs)
                {
                    var subDirDest = Path.Combine(destDirName, subdir.Name);
                    subdir.Copy(subDirDest, true);
                }
            }
        }
    }
}
