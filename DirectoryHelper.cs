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

        public static void ReplaceTextFileContents(string path, params (string find, string replace)[] replacements)
        {
            var projectText = File.ReadAllText(path);
            var newProjectText = projectText;
            foreach (var pair in replacements)
            {
                newProjectText = newProjectText.Replace(pair.find, pair.replace);
            }
            if (!string.Equals(projectText, newProjectText, StringComparison.Ordinal))
            {
                File.WriteAllText(path, newProjectText, Program.DefaultEncoding);
            }
        }

        public static bool IsDirectory(string path)
        {
            var attributes = File.GetAttributes(path);
            return attributes.HasFlag(FileAttributes.Directory);
        }

        public static void MoveFileOrDirectory(string sourcePath, string targetPath)
        {
            if (IsDirectory(sourcePath))
            {
                Directory.Move(sourcePath, targetPath);
            }
            else
            {
                File.Move(sourcePath, targetPath);
            }
        }

        public static void ReplaceFileOrDirectoryName(string path, params (string find, string replace)[] replacements)
        {
            var folderPath = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);

            foreach (var pair in replacements)
            {
                fileName = fileName.Replace(pair.find, pair.replace);
            }

            var newPath = Path.Combine(folderPath, fileName);
            if (!string.Equals(path, newPath, StringComparison.Ordinal))
            {
                MoveFileOrDirectory(path, newPath);
            }
        }

        public static void ReplaceDirectoryContents(string path, string[] extensions, StringComparison comparison, SearchOption searchOption, bool rename, params (string find, string replace)[] replacements)
        {
            foreach (var filePath in Directory.GetFiles(path, "*.*", searchOption))
            {
                if (extensions == null || extensions.Any(x => string.Equals(Path.GetExtension(filePath), x, comparison)))
                {
                    ReplaceTextFileContents(filePath, replacements);
                }

                if (rename)
                {
                    ReplaceFileOrDirectoryName(filePath, replacements);
                }
            }

            if (rename)
            {
                foreach (var folderPath in Directory.GetDirectories(path, "*", searchOption)
                                                    .OrderByDescending(x => x.Length))
                {
                    ReplaceFileOrDirectoryName(folderPath, replacements);
                }
            }
        }
    }
}
