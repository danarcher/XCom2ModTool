using System;
using System.IO;

namespace XCom2ModTool
{
    internal class XCom2Edition
    {
        private static readonly string XComGameFolderName = "XComGame";
        private static readonly string ModsFolderName = "Mods";
        private static readonly string SdkDevelopmentFolderName = "Development";
        private static readonly string SdkSourceCodeFolderName = "Src";
        private static readonly string SdkOriginalSourceCodeFolderName = "SrcOrig";
        private static readonly string SdkScriptFolderName = "Script";
        private static readonly string SdkBinariesFolderName = "binaries";
        private static readonly string SdkWin64FolderName = "win64";
        private static readonly string SdkCompilerName = "XComGame.com";
        private static readonly string SdkEditorName = "XComGame.exe";
        private static readonly string ConfigFolderName = "Config";
        private static readonly string UserConfigPathInDocuments = System.IO.Path.Combine("My Games", "XCOM2", XComGameFolderName, ConfigFolderName);
        private static readonly string LogPathInDocuments = System.IO.Path.Combine("My Games", "XCOM2", XComGameFolderName, "Logs", "Launch.log");

        private string path;
        private string sdkPath;

        public XCom2Edition(string displayName, string steamAppName, string subFolderName, string sdkSteamAppName)
        {
            DisplayName = displayName;

            if (Steam.TryFindApp(steamAppName, out string path))
            {
                if (string.IsNullOrEmpty(subFolderName))
                {
                    this.path = path;
                }
                else
                {
                    path = System.IO.Path.Combine(path, subFolderName);
                    if (Directory.Exists(path))
                    {
                        this.path = path;
                    }
                }
            }

            if (Steam.TryFindApp(sdkSteamAppName, out path))
            {
                sdkPath = path;
            }
        }

        public bool IsInstalled => !string.IsNullOrEmpty(path);
        public bool IsSdkInstalled => !string.IsNullOrEmpty(sdkPath);

        public string DisplayName { get; }
        public string SdkDisplayName => $"{DisplayName} SDK";

        public string Path => ConditionalPath(IsInstalled, path, DisplayName);

        public string SdkPath => ConditionalPath(IsSdkInstalled, sdkPath, SdkDisplayName);

        public string XComGamePath => System.IO.Path.Combine(Path, XComGameFolderName);

        public string SdkXComGamePath => System.IO.Path.Combine(SdkPath, XComGameFolderName);

        public string ModsPath => System.IO.Path.Combine(XComGamePath, ModsFolderName);

        public string SdkModsPath => System.IO.Path.Combine(SdkXComGamePath, ModsFolderName);

        public string SdkSourceCodePath => System.IO.Path.Combine(SdkPath, SdkDevelopmentFolderName, SdkSourceCodeFolderName);

        public string SdkOriginalSourceCodePath => System.IO.Path.Combine(SdkPath, SdkDevelopmentFolderName, SdkOriginalSourceCodeFolderName);

        public string SdkXComGameCompiledScriptPath => System.IO.Path.Combine(SdkXComGamePath, SdkScriptFolderName);

        public string SdkCompilerPath => System.IO.Path.Combine(SdkPath, SdkBinariesFolderName, SdkWin64FolderName, SdkCompilerName);

        public string UserConfigPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), UserConfigPathInDocuments);

        public string UserLogPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), LogPathInDocuments);

        public string EditorPath => System.IO.Path.Combine(SdkPath, SdkBinariesFolderName, SdkWin64FolderName, SdkEditorName);

        public string GetModStagingPath(ModInfo modInfo)
        {
            return System.IO.Path.Combine(SdkModsPath, modInfo.RootFolder);
        }

        public string GetModInstallPath(ModInfo modInfo)
        {
            return System.IO.Path.Combine(ModsPath, modInfo.RootFolder);
        }

        private string ConditionalPath(bool condition, string value, string displayName)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"{displayName} is not installed");
            }
            return value;
        }
    }
}
