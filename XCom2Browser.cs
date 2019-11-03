using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace XCom2ModTool
{
    internal class XCom2Browser
    {
        private static readonly string EditorName = "editor";
        private static readonly string[] EditorArguments = new[] { "editor", "-noscriptcompile", "-nogadwarning" };

        public static (string name, Func<XCom2Edition, string> describe, Func<XCom2Edition, string> getPath, string[] arguments)[] GetFolders()
        {
            return new (string, Func<XCom2Edition, string>, Func<XCom2Edition, string>, string[])[]
            {
                ("xcom2", x => $"The {x.DisplayName} folder", x => x.Path, null),
                ("mods", x => $"The {x.DisplayName} mods folder", x => x.ModsPath, null),
                ("sdk", x => $"The {x.SdkDisplayName} folder", x => x.SdkPath, null),
                ("sdk-mods", x => $"The {x.SdkDisplayName} mods folder", x => x.SdkModsPath, null),
                ("config", x => $"The current user's {x.DisplayName} config folder", x => x.UserConfigPath, null),
                ("log", x => $"The current user's {x.DisplayName} log file", x => x.UserLogPath, null),
                ("save", x => $"The current user's {x.DisplayName} save folder", x => x.UserSavePath, null),
                ("int", x => $"The {x.DisplayName} INT localization folder", x => x.IntPath, null),
                (EditorName, x => $"The {x.DisplayName} Editor", x => x.EditorPath, EditorArguments),
            };
        }

        public static void Browse(string name, XCom2Edition edition)
        {
            var folder = GetFolders().FirstOrDefault(x => string.Equals(name, x.name, StringComparison.OrdinalIgnoreCase));
            PrepareToBrowse(folder.name, edition);
            var path = folder.getPath(edition);
            if (folder.arguments?.Length > 0)
            {
                Report.Verbose($"> \"{path}\" {PathHelper.EscapeAndJoinArguments(folder.arguments)}");
                Process.Start(path, PathHelper.EscapeAndJoinArguments(folder.arguments));
            }
            else
            {
                Report.Verbose($"> \"{path}\"");
                Process.Start(path);
            }
        }

        private static void PrepareToBrowse(string name, XCom2Edition edition)
        {
            if (name == EditorName)
            {
                Report.Verbose($"Deleting {edition.SdkDisplayName} mods");
                DirectoryHelper.DeleteDirectoryContents(edition.SdkModsPath);
            }
        }

        public static void CopyToClipboard(string name, XCom2Edition edition)
        {
            var folder = GetFolders().FirstOrDefault(x => string.Equals(name, x.name, StringComparison.OrdinalIgnoreCase));
            Clipboard.SetText(folder.getPath(edition));
        }
    }
}