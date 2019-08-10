using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;

namespace XCom2ModTool
{
    internal static class Steam
    {
        private static string AppsFolderName = "steamapps";
        private static string CommonFolderName = "common";
        private static string LibraryFoldersFileName = "libraryfolders.vdf";

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
            var contents = VdfConvert.Deserialize(File.ReadAllText(path));
            var paths = new List<string>();
            foreach (var item in contents.Value)
            {
                var property = item as VProperty;
                if (property != null &&
                    int.TryParse(property.Key, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
                {
                    var value = property.Value as VValue;
                    if (value != null)
                    {
                        var text = value.Value as string;
                        if (text != null)
                        {
                            paths.Add(text);
                        }
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
    }
}
