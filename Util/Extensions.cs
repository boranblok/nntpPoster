using System;
using System.IO;
using log4net;

namespace Util
{
    public static class Extensions
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static Int64 Size(this FileSystemInfo fsi)
        {
            FileAttributes attributes = File.GetAttributes(fsi.FullName);
            if (attributes.HasFlag(FileAttributes.Directory))
                return new DirectoryInfo(fsi.FullName).Size();
            else
                return new FileInfo(fsi.FullName).Length;
        }

        public static Int64 Size(this DirectoryInfo d)
        {
            Int64 size = 0;
            foreach (FileInfo fi in d.GetFiles())
            {
                size += fi.Length;
            }

            foreach (DirectoryInfo di in d.GetDirectories())
            {
                size += di.Size();
            }

            return (size);
        }

        public static String GetRelativePath(this DirectoryInfo d, FileInfo f)
        {
            String fileFullName = f.FullName;
            String dirFullName = d.FullName;
            if (!fileFullName.StartsWith(dirFullName))
                throw new ArgumentException("The file is not in the directory or a subdirectory.");
            return fileFullName.Substring(dirFullName.Length);
        }

        public static String NameWithoutExtension(this FileSystemInfo fsi)
        {
            FileAttributes attributes = File.GetAttributes(fsi.FullName);
            if (attributes.HasFlag(FileAttributes.Directory))
                return new DirectoryInfo(fsi.FullName).Name;
            else
                return new FileInfo(fsi.FullName).NameWithoutExtension();
        }

        public static String NameWithoutExtension(this FileInfo f)
        {
            Int32 extensionPosition = f.Name.LastIndexOf(f.Extension, StringComparison.Ordinal);
            if (extensionPosition > 0)
                return f.Name.Substring(0, extensionPosition);
            return f.Name;
        }

        public static void Copy(this DirectoryInfo sourceDirectory, String destDirName, Boolean copySubDirs)
        {
            DirectoryInfo[] dirs = sourceDirectory.GetDirectories();

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

            FileInfo[] files = sourceDirectory.GetFiles();
            foreach (FileInfo file in files)
            {
                String temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    String subDirDest = Path.Combine(destDirName, subdir.Name);
                    subdir.Copy(subDirDest, copySubDirs);
                }
            }
        }

        public static FileSystemInfo Move(this FileSystemInfo fsi, DirectoryInfo destination)
        {
            FileSystemInfo movedFsi;
            FileAttributes attributes = File.GetAttributes(fsi.FullName);
            if(!destination.Exists)
                Directory.CreateDirectory(destination.FullName);
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                movedFsi = MoveFolder(fsi.FullName, destination);
            }
            else
            {
                movedFsi = MoveFile(fsi.FullName, destination);
            }
            return movedFsi;
        }

        private static FileSystemInfo MoveFolder(String sourceFolder, DirectoryInfo destination)
        {
            DirectoryInfo toMove = new DirectoryInfo(sourceFolder);
            String destinationFolder = Path.Combine(destination.FullName, toMove.Name);
            DirectoryInfo moved = new DirectoryInfo(destinationFolder);
            if (moved.Exists)
            {
                log.WarnFormat("The backup folder for '{0}' already existed. Overwriting!", toMove.Name);
                moved.Delete();
            }
            Directory.Move(sourceFolder, destinationFolder);
            return moved;
        }

        private static FileSystemInfo MoveFile(String sourceFile, DirectoryInfo destination)
        {
            FileInfo toMove = new FileInfo(sourceFile);
            String destinationFile = Path.Combine(destination.FullName, toMove.Name);
            FileSystemInfo moved = new FileInfo(destinationFile);
            if (moved.Exists)
            {
                log.WarnFormat("The backup folder for '{0}' already existed. Overwriting!", toMove.Name);
                moved.Delete();
            }
            File.Move(sourceFile, destinationFile);
            return moved;
        }
    }
}
