using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace XCom2ModTool
{
    internal class XCom2Browser
    {
        private static readonly string EditorName = "editor";
        private static readonly string WotcEditorName = "wotc-editor";
        private static readonly string[] EditorArguments = new[] { "editor", "-noscriptcompile", "-nogadwarning" };

        public static (string name, string description, Func<string> getPath, string[] arguments)[] GetPaths()
        {
            return new (string, string, Func<string>, string[])[]
            {
                ("xcom2", $"The {XCom2.Base.DisplayName} folder", () => XCom2.Base.Path, null),
                ("mods", $"The {XCom2.Base.DisplayName} mods folder", () => XCom2.Base.ModsPath, null),
                ("sdk", $"The {XCom2.Base.SdkDisplayName} folder", () => XCom2.Base.SdkPath, null),
                ("sdk-mods", $"The {XCom2.Base.SdkDisplayName} mods folder", () => XCom2.Base.SdkModsPath, null),
                ("config", $"The current user's {XCom2.Base.DisplayName} config folder", () => XCom2.Base.UserConfigPath, null),
                ("log", $"The current user's {XCom2.Base.DisplayName} log file", () => XCom2.Base.UserLogPath, null),
                (EditorName, $"The {XCom2.Base.DisplayName} Editor", () => XCom2.Base.EditorPath, EditorArguments),
                ("wotc", $"The {XCom2.Wotc.DisplayName} folder", () => XCom2.Wotc.Path, null),
                ("wotc-mods", $"The {XCom2.Wotc.DisplayName} mods folder", () => XCom2.Wotc.ModsPath, null),
                ("wotc-sdk", $"The {XCom2.Wotc.SdkDisplayName} folder", () => XCom2.Wotc.SdkPath, null),
                ("wotc-sdk-mods", $"The {XCom2.Wotc.SdkDisplayName} mods folder", () => XCom2.Wotc.SdkModsPath, null),
                (WotcEditorName, $"The {XCom2.Wotc.DisplayName} Editor", () => XCom2.Wotc.EditorPath, EditorArguments),
            };
        }

        public static void Browse(string name)
        {
            var path = GetPaths().FirstOrDefault(x => string.Equals(name, x.name, StringComparison.OrdinalIgnoreCase));
            PrepareToBrowse(path);
            if (path.arguments?.Length > 0)
            {
                Report.Verbose($"> \"{path.getPath()}\" {PathHelper.EscapeAndJoinArguments(path.arguments)}");
                Process.Start(path.getPath(), PathHelper.EscapeAndJoinArguments(path.arguments));
            }
            else
            {
                Report.Verbose($"> \"{path.getPath()}\"");
                Process.Start(path.getPath());
            }
        }

        private static void PrepareToBrowse((string name, string description, Func<string> getPath, string[] arguments) path)
        {
            if (path.name == EditorName)
            {
                Report.Verbose($"Deleting {XCom2.Base.SdkDisplayName} mods");
                DirectoryHelper.DeleteDirectoryContents(XCom2.Base.SdkModsPath);
            }
            else if (path.name == WotcEditorName)
            {
                Report.Verbose($"Deleting {XCom2.Wotc.SdkDisplayName} mods");
                DirectoryHelper.DeleteDirectoryContents(XCom2.Wotc.SdkModsPath);
            }
        }

        public static void CopyToClipboard(string name)
        {
            var folder = GetPaths().FirstOrDefault(x => string.Equals(name, x.name, StringComparison.OrdinalIgnoreCase));
            Clipboard.SetText(folder.getPath());
        }
    }
}