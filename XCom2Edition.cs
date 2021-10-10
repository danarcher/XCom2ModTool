using System;
using System.IO;
using System.Linq;

namespace XCom2ModTool
{
    internal class XCom2Edition
    {
        private static readonly string XComGameFolderName = "XComGame";
        private static readonly string ModsFolderName = "Mods";
        private static readonly string LocalizationFolderName = "Localization";
        private static readonly string IntFolderName = "INT";
        private static readonly string SdkDevelopmentFolderName = "Development";
        private static readonly string SdkSourceCodeFolderName = "Src";
        private static readonly string SdkOriginalSourceCodeFolderName = "SrcOrig";
        private static readonly string SdkSourceCodeXComGameFolderName = "XComGame";
        private static readonly string SdkSourceCodeClassesFolderName = "Classes";
        private static readonly string SdkScriptFolderName = "Script";
        private static readonly string SdkBinariesFolderName = "binaries";
        private static readonly string SdkWin64FolderName = "win64";
        private static readonly string SdkCompilerName = "XComGame.com";
        private static readonly string SdkEditorName = "XComGame.exe";
        private static readonly string MyGamesFolderName = "My Games";
        private static readonly string ConfigFolderName = "Config";
        private static readonly string LogsFolderName = "Logs";
        private static readonly string SaveFolderName = "SaveData";
        private static readonly string LogFileName = "Launch.log";
        private static readonly string ShaderCacheFileNameFormat = "{0}_ModShaderCache" + ModInfo.PackageExtension;
        private static readonly string HighlanderSourceCodeFolderName = "Src";

        private string path;
        private string sdkPath;
        private string userGameFolderName;
        private string highlanderName;

        public XCom2Edition(string internalName, string displayName, string steamAppName, string subFolderName, string sdkSteamAppName, string userGameFolderName, string highlanderName, bool isExpansion = false)
        {
            InternalName = internalName;
            DisplayName = displayName;
            IsExpansion = isExpansion;
            this.userGameFolderName = userGameFolderName;
            this.highlanderName = highlanderName;

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

        public string InternalName { get; }
        public string DisplayName { get; }
        public string SdkDisplayName => $"{DisplayName} SDK";
        public bool IsExpansion { get; }

        public string Path => ConditionalPath(IsInstalled, path, DisplayName);

        public string SdkPath => ConditionalPath(IsSdkInstalled, sdkPath, SdkDisplayName);

        public string XComGamePath => Combine(Path, XComGameFolderName);

        public string IntPath => Combine(XComGamePath, LocalizationFolderName, IntFolderName);

        public string SdkXComGamePath => Combine(SdkPath, XComGameFolderName);

        public string ModsPath => Combine(XComGamePath, ModsFolderName);

        public string SdkModsPath => Combine(SdkXComGamePath, ModsFolderName);

        public string SdkSourceCodePath => Combine(SdkPath, SdkDevelopmentFolderName, SdkSourceCodeFolderName);

        public string SdkSourceCodeClassesPath => Combine(SdkPath, SdkDevelopmentFolderName, SdkSourceCodeFolderName, SdkSourceCodeXComGameFolderName, SdkSourceCodeClassesFolderName);

        public string SdkOriginalSourceCodePath => Combine(SdkPath, SdkDevelopmentFolderName, SdkOriginalSourceCodeFolderName);

        public string SdkXComGameCompiledScriptPath => Combine(SdkXComGamePath, SdkScriptFolderName);

        public string SdkCompilerPath => Combine(SdkPath, SdkBinariesFolderName, SdkWin64FolderName, SdkCompilerName);

        public string UserPath => Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), MyGamesFolderName, userGameFolderName, XComGameFolderName);

        public string UserConfigPath => Combine(UserPath, ConfigFolderName);

        public string UserLogPath => Combine(UserPath, LogsFolderName, LogFileName);

        public string UserSavePath => Combine(UserPath, SaveFolderName);

        public string EditorPath => System.IO.Path.Combine(SdkPath, SdkBinariesFolderName, SdkWin64FolderName, SdkEditorName);

        public string SdkHighlanderSourceCodeFolderName => highlanderName;

        public string GetModStagingPath(ModInfo modInfo)
        {
            return System.IO.Path.Combine(SdkModsPath, modInfo.RootFolder);
        }

        public string GetModInstallPath(ModInfo modInfo)
        {
            return System.IO.Path.Combine(ModsPath, modInfo.RootFolder);
        }

        public string GetModShaderCacheStagingPath(ModInfo modInfo)
        {
            return System.IO.Path.Combine(GetModStagingPath(modInfo), ModInfo.ContentFolder, string.Format(ShaderCacheFileNameFormat, modInfo.ModName));
        }

        public string GetModShaderCacheInstallPath(ModInfo modInfo)
        {
            return System.IO.Path.Combine(GetModInstallPath(modInfo), ModInfo.ContentFolder, string.Format(ShaderCacheFileNameFormat, modInfo.ModName));
        }

        public string GetHighlanderModSourceCodePath()
        {
            var highlanderPaths = Steam.FindAppWorkshopItemPaths(XCom2.SteamAppId)
                                       .Append(Combine(XComGamePath, ModsFolderName, highlanderName))
                                       .Select(x => Combine(x, highlanderName + ModMetadata.Extension))
                                       .Where(x => File.Exists(x))
                                       .Select(x => System.IO.Path.GetDirectoryName(x))
                                       .ToArray();
            if (highlanderPaths.Length == 0)
            {
                throw new Exception($"Highlander not found in {DisplayName} Mods or Steam workshop folders");
            }
            if (highlanderPaths.Length > 1)
            {
                var details = highlanderPaths.Zip(Enumerable.Range(1, highlanderPaths.Length), (x, y) => $"{y}. {x}");
                throw new DetailedException($"Multiple ({highlanderPaths.Length}) highlanders found in {DisplayName} Mods folder and/or Steam workshop folders", details.ToArray());
            }
            return Combine(highlanderPaths.Single(), HighlanderSourceCodeFolderName);
        }

        private string Combine(params string[] paths) => System.IO.Path.Combine(paths);

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
