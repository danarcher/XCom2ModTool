using System;
using System.IO;
using System.Linq;

namespace XCom2ModTool
{
    internal static class DirectoryHelper
    {
        public static int Copy(string sourcePath, string targetPath)
        {
            Directory.CreateDirectory(targetPath);

            foreach (var sourceDirectory in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                var targetDirectory = sourceDirectory.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(targetDirectory);
            }

            var count = 0;
            foreach (var sourceFile in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                var targetFile = sourceFile.Replace(sourcePath, targetPath);
                File.Copy(sourceFile, targetFile, true);
                ++count;
            }
            return count;
        }

        public static void DeleteByExtension(string path, SearchOption searchOption, StringComparison comparison, params string[] extensions)
        {
            foreach (var file in Directory.GetFiles(path, "*.*", searchOption)
                                          .Select(x => new FileInfo(x))
                                          .Where(x => extensions.Any(y => string.Equals(x.Extension, y, comparison))))
            {
                File.SetAttributes(file.FullName, FileAttributes.Normal);
                File.Delete(file.FullName);
            }
        }

        // Adapted from https://stackoverflow.com/a/326153
        public static string GetExactPathName(string pathName)
        {
            if (!File.Exists(pathName) && !Directory.Exists(pathName))
            {
                return pathName;
            }

            var di = new DirectoryInfo(pathName);

            if (di.Parent != null)
            {
                return Path.Combine(
                    GetExactPathName(di.Parent.FullName),
                    di.Parent.GetFileSystemInfos(di.Name)[0].Name);
            }
            else
            {
                return di.Name.ToUpper();
            }
        }
    }
}
