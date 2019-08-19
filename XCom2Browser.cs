using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace XCom2ModTool
{
    internal class XCom2Browser
    {
        public static (string name, string description, Func<string> getPath)[] GetFolders()
        {
            return new (string, string, Func<string>)[]
            {
                ("xcom2", $"The {XCom2.Base.DisplayName} folder", () => XCom2.Base.Path),
                ("mods", $"The {XCom2.Base.DisplayName} mods folder", () => XCom2.Base.ModsPath),
                ("sdk", $"The {XCom2.Base.SdkDisplayName} folder", () => XCom2.Base.SdkPath),
                ("sdk-mods", $"The {XCom2.Base.SdkDisplayName} mods folder", () => XCom2.Base.SdkModsPath),
                ("config", $"The current user's {XCom2.Base.DisplayName} config folder", () => XCom2.Base.UserConfigPath),
                ("log", $"The current user's {XCom2.Base.DisplayName} log file", () => XCom2.Base.UserLogPath),
                ("wotc", $"The {XCom2.Wotc.DisplayName} folder", () => XCom2.Wotc.Path),
                ("wotc-mods", $"The {XCom2.Wotc.DisplayName} mods folder", () => XCom2.Wotc.ModsPath),
                ("wotc-sdk", $"The {XCom2.Wotc.SdkDisplayName} folder", () => XCom2.Wotc.SdkPath),
                ("wotc-sdk-mods", $"The {XCom2.Wotc.SdkDisplayName} mods folder", () => XCom2.Wotc.SdkModsPath),
            };
        }

        public static void Browse(string name)
        {
            var folder = GetFolders().FirstOrDefault(x => string.Equals(name, x.name, StringComparison.OrdinalIgnoreCase));
            Process.Start(folder.getPath());
        }

        public static void CopyToClipboard(string name)
        {
            var folder = GetFolders().FirstOrDefault(x => string.Equals(name, x.name, StringComparison.OrdinalIgnoreCase));
            Clipboard.SetText(folder.getPath());
        }
    }
}