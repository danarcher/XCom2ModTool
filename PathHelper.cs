using System;
using System.Globalization;
using System.IO;

namespace XCom2ModTool
{
    internal class PathHelper
    {
        public static bool IsAbsolute(string path)
        {
            return Path.IsPathRooted(path);
        }

        public static bool IsRelative(string path)
        {
            return !IsAbsolute(path);
        }

        public static string MakeAbsolute(string path)
        {
            return Path.GetFullPath(path);
        }

        public static string MakeAbsolute(string path, string parentFolderPath)
        {
            if (IsAbsolute(path))
            {
                return path;
            }

            return MakeAbsolute(Path.Combine(parentFolderPath, path));
        }

        public static string MakeRelative(string path, string parentFolderPath)
        {
            if (IsRelative(path))
            {
                throw new ArgumentException($"{nameof(path)} is a relative path");
            }
            if (IsRelative(parentFolderPath))
            {
                throw new ArgumentException($"{nameof(parentFolderPath)} is a relative path");
            }

            var pathUri = new Uri(path);
            if (!parentFolderPath.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)))
            {
                parentFolderPath += Path.DirectorySeparatorChar;
            }
            var folderUri = new Uri(parentFolderPath);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
