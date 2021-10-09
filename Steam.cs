using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Win32;

namespace XCom2ModTool
{
    internal static class Steam
    {
        private static readonly string AppsFolderName = "steamapps";
        private static readonly string CommonFolderName = "common";
        private static readonly string LibraryFoldersFileName = "libraryfolders.vdf";
        private static readonly string WorkshopFolderName = "workshop";
        private static readonly string WorkshopContentFolderName = "content";
        private static readonly string PathDelimeter = "\"path\"";

        public static string InstallPath { get; } = FindInstallPath();
        public static string[] LibraryPaths { get; } = FindLibraryPaths();

        private static string FindInstallPath()
        {
            // If we we had any "Any CPU" or x64 build, this could/would be under HKLM\Software\Wow6432Node\Valve\Steam.
            using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Valve\Steam"))
            {
                return (string)key.GetValue("InstallPath");
            }
        }

        private static string[] FindLibraryPaths()
        {
            var path = Path.Combine(InstallPath, AppsFolderName, LibraryFoldersFileName);
            var lines = File.ReadAllLines(path);
            var paths = new List<string>();
            foreach (var line in lines)
            {
                var pathIndex = line.IndexOf(PathDelimeter);
                if (pathIndex >= 0)
                {
                    var text = line.Substring(pathIndex + PathDelimeter.Length + 1);
                    text = text.Trim().Trim('\"').Replace("\\\\", "\\");
                    if (!string.IsNullOrEmpty(text))
                    {
                        paths.Add(text);
                    }
                }
            }
            return paths.ToArray();
        }

        public static bool TryFindApp(string appName, out string path)
        {
            foreach (var libraryPath in LibraryPaths)
            {
                path = Path.Combine(libraryPath, AppsFolderName, CommonFolderName, appName);
                if (Directory.Exists(path))
                {
                    return true;
                }
            }

            path = null;
            return false;
        }

        public static string FindApp(string appName)
        {
            if (!TryFindApp(appName, out string path))
            {
                throw new DirectoryNotFoundException($"Steam install of {appName} not found");
            }
            return path;
        }

        public static IEnumerable<string> FindAppWorkshopPaths(int appId)
        {
            var pathsFound = new List<string>();
            foreach (var libraryPath in LibraryPaths)
            {
                var path = Path.Combine(libraryPath, AppsFolderName, WorkshopFolderName, WorkshopContentFolderName, appId.ToString(CultureInfo.InvariantCulture));
                if (Directory.Exists(path))
                {
                    yield return path;
                }
            }
        }

        public static IEnumerable<string> FindAppWorkshopItemPaths(int appId)
        {
            foreach (var workshopPath in FindAppWorkshopPaths(appId))
            {
                foreach (var itemPath in Directory.EnumerateDirectories(workshopPath, "*", SearchOption.TopDirectoryOnly))
                {
                    if (int.TryParse(Path.GetFileName(itemPath), NumberStyles.None, CultureInfo.InvariantCulture, out int itemNumber))
                    {
                        yield return itemPath;
                    }
                }
            }
        }
    }
}
