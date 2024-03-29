﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using XCom2ModTool.UnrealPackages;

namespace XCom2ModTool
{
    internal class ModBuilder
    {
        private static readonly string ScriptFolderName = "Script";
        private static readonly string ScriptExtension = ".u";
        private static readonly string CompiledScriptManifestName = "Manifest.txt";
        private static readonly string KeyStandardPackageCompiledScriptFileName = "XComGame" + ScriptExtension;
        private static readonly string HighlanderIndicativeExportName = "CHXComGameVersionTemplate";

        private static readonly string[] StandardSourceCodeFolderNames = new[]
        {
            "AkAudio",
            "Core",
            "DLC_1",
            "DLC_2",
            "DLC_3",
            "Engine",
            "GameFramework",
            "GFxUI",
            "GFxUIEditor",
            "IpDrv",
            "OnlineSubsystemSteamworks",
            "TLE",
            "UnrealEd",
            "XComEditor",
            "XComGame",
        };

        private static readonly string[] StandardCompiledScripts = StandardSourceCodeFolderNames.Select(x => x + ScriptExtension).Concat(new[]
        {
            "DO_NOT_DELETE.TXT",
            CompiledScriptManifestName,
        }).ToArray();

        private static readonly string[] StandardManifestModules = StandardSourceCodeFolderNames.Concat(new[]
        {
            "WinDrv",
            "XAudio2",
        }).ToArray();

        private CancellationToken cancellation;
        private XCom2Edition edition;
        private Compiler compiler;
        private ModInfo modInfo;
        private ModProject modProject;
        private string modStagingPath;
        private string modInstallPath;
        private string modShaderCacheStagingPath = null;
        private string modShaderCacheInstallPath = null;

        private bool modHasSourceCode;
        private bool modHasShaderContent;
        private string modStagingCompiledScriptFolderPath = null;
        private string modSdkCompiledScriptPath = null;
        private string modStagingCompiledScriptFilePath = null;

        public ModBuilder(ModInfo modInfo, XCom2Edition edition, CancellationToken cancellation)
        {
            this.modInfo = modInfo;
            this.edition = edition;
            this.cancellation = cancellation;
            compiler = new Compiler(edition);
            modStagingPath = edition.GetModStagingPath(modInfo);
            modInstallPath = edition.GetModInstallPath(modInfo);
            modShaderCacheStagingPath = edition.GetModShaderCacheStagingPath(modInfo);
            modShaderCacheInstallPath = edition.GetModShaderCacheInstallPath(modInfo);
            modHasSourceCode = modInfo.HasSourceCode();
            modHasShaderContent = modInfo.HasShaderContent();
            modStagingCompiledScriptFolderPath = Path.Combine(modStagingPath, ScriptFolderName);
            modSdkCompiledScriptPath = Path.Combine(edition.SdkXComGameCompiledScriptPath, modInfo.ModName + ScriptExtension);
            modStagingCompiledScriptFilePath = Path.Combine(modStagingCompiledScriptFolderPath, modInfo.ModName + ScriptExtension);
            compiler.ReplacePaths.Add(edition.SdkSourceCodePath, modInfo.SourceCodePath);
        }

        public void Clean()
        {
            CleanModStaging();
        }

        private void ThrowIfCancelled() => cancellation.ThrowIfCancellationRequested();

        public void Build(ModBuildType buildType)
        {
            Report.Verbose($"{buildType} build of {modInfo.ModName}");

            // Load project first, to check folder structure is as we expect before we start moving files.
            Report.Verbose("Loading project");
            modProject = ModProject.Load(modInfo, edition);

            if (!string.Equals(modProject.Title, modInfo.ModName, StringComparison.Ordinal))
            {
                Report.Warning($"Mod name {modInfo.ModName} does not match title '{modProject.Title}' in project {modInfo.ProjectName}");
            }

            // Switching between building with/without the highlander requires a full build.
            if (IsSdkBuiltWithHighlander() != Settings.Default.Highlander)
            {
                var highlander = Settings.Default.Highlander;
                Report.Warning($"This is a {(highlander ? "" : "non-")}highlander build, but {KeyStandardPackageCompiledScriptFileName} was {(highlander ? "not " : "")}built with the highlander; switching to full build");
                buildType = ModBuildType.Full;
            }
            else
            {
                var highlander = Settings.Default.Highlander;
                Report.Verbose($"This is a {(highlander ? "" : "non-")}highlander build, as is {KeyStandardPackageCompiledScriptFileName}");
            }

            CleanModStaging();
            ThrowIfCancelled();

            CleanSdkMods();
            Directory.CreateDirectory(modStagingPath);
            StageModFolder(ModInfo.SourceCodeFolder);
            StageModFolder(ModInfo.ConfigFolder);
            StageModFolder(ModInfo.LocalizationFolder);
            StageModFolder(ModInfo.ContentFolder);
            StageModMetadata();
            ThrowIfCancelled();

            if (modHasSourceCode)
            {
                switch (buildType)
                {
                    case ModBuildType.Full:
                        CleanSdkSourceCode();
                        RestoreSdkSourceCode();
                        CopyModSourceCodeToSdk();
                        CleanSdkCompiledScripts();
                        CompileGame();
                        break;
                    case ModBuildType.Fast:
                        CleanSdkSourceCode();
                        RestoreSdkSourceCode();
                        CopyModSourceCodeToSdk();
                        CleanModSdkCompiledScripts();
                        CompileGame();
                        break;
                    case ModBuildType.Smart:
                        var flags = GetBuiltStandardPackageFlags();
                        if (!flags.HasValue)
                        {
                            Report.Verbose($"{KeyStandardPackageCompiledScriptFileName} invalid or not found, switching to full build");
                            goto case ModBuildType.Full;
                        }
                        if (Settings.Default.Debug != flags.Value.HasFlag(PackageFlags.Debug))
                        {
                            Settings.Default.Debug = !Settings.Default.Debug;
                            Report.Verbose($"Detected {(Settings.Default.Debug ? "debug" : "release")} build of {KeyStandardPackageCompiledScriptFileName}");
                        }
                        SmartCleanSdkSourceCode();
                        CopyModSourceCodeToSdk();
                        SmartCleanSdkCompiledScripts();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(buildType));
                }

                CompileMod();
                if (modHasShaderContent)
                {
                    switch (buildType)
                    {
                        case ModBuildType.Full:
                        case ModBuildType.Fast:
                            CompileShaders();
                            break;
                        case ModBuildType.Smart:
                            if (IsDeployedShaderCacheUpToDate())
                            {
                                CopyDeployedShaderCacheToStaging();
                            }
                            else
                            {
                                CompileShaders();
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(buildType));
                    }
                }
                StageModCompiledScripts();
                SmartCleanSdkSourceCode();
            }

            DeployMod();
        }

        private bool IsSdkBuiltWithHighlander()
        {
            var path = Path.Combine(edition.SdkXComGameCompiledScriptPath, KeyStandardPackageCompiledScriptFileName);
            using (var reader = new PackageReader(path))
            {
                var header = reader.ReadHeader();
                return header.Exports.Any(x => string.Equals(x.Name, HighlanderIndicativeExportName, StringComparison.Ordinal));
            }
        }

        private void CleanSdkMods()
        {
            Report.Verbose("Cleaning SDK mods");
            DirectoryHelper.DeleteDirectoryContents(edition.SdkModsPath);
        }

        private void CleanModStaging()
        {
            if (Directory.Exists(modStagingPath))
            {
                Report.Verbose($"Cleaning staging folder");
                DirectoryHelper.Delete(modStagingPath);
            }
            else
            {
                Report.Verbose($"Staging is clean");
            }
        }

        private bool StageModFolder(string folderName)
        {
            var sourcePath = Path.Combine(modInfo.InnerPath, folderName);
            if (Directory.Exists(sourcePath))
            {
                Report.Verbose($"Staging {folderName}");
                var targetPath = Path.Combine(modStagingPath, folderName);
                DirectoryHelper.CopyDirectory(sourcePath, targetPath);
                return true;
            }
            return false;
        }

        private void StageModMetadata()
        {
            Report.Verbose("Writing metadata");
            ModMetadata.Save(modProject, Path.Combine(modStagingPath, modInfo.ModName + ModMetadata.Extension));
        }

        private void CleanSdkSourceCode()
        {
            Report.Verbose("Cleaning SDK source");
            DirectoryHelper.Delete(edition.SdkSourceCodePath);
            Directory.CreateDirectory(edition.SdkSourceCodePath);
        }

        private void SmartCleanSdkSourceCode()
        {
            Report.Verbose("Smart-cleaning SDK source");
            foreach (var folderPath in Directory.GetDirectories(edition.SdkSourceCodePath))
            {
                var folderName = Path.GetFileName(folderPath);
                if (!StandardSourceCodeFolderNames.Any(x => string.Equals(x, folderName, StringComparison.OrdinalIgnoreCase)) &&
                    (!Settings.Default.Highlander || !string.Equals(folderName, edition.SdkHighlanderSourceCodeFolderName)))
                {
                    Report.Verbose($"  Deleting non-standard source {folderName}", Verbosity.Loquacious);
                    DirectoryHelper.Delete(folderPath);
                }
            }

            foreach (var filePath in Directory.GetFiles(edition.SdkSourceCodePath))
            {
                Report.Verbose($"  Deleting non-standard file {Path.GetFileName(filePath)}");
                DirectoryHelper.Delete(filePath);
            }
        }

        private void RestoreSdkSourceCode()
        {
            Report.Verbose("Restoring SDK source");
            var count = DirectoryHelper.CopyDirectory(edition.SdkOriginalSourceCodePath, edition.SdkSourceCodePath);
            Report.Verbose($"Restored {count} files");
            if (Settings.Default.Highlander)
            {
                var highlanderSourceCodePath = edition.GetHighlanderModSourceCodePath();
                Report.Verbose($"Restoring highlander source from {highlanderSourceCodePath}");
                count = DirectoryHelper.CopyDirectory(highlanderSourceCodePath, edition.SdkSourceCodePath);
                Report.Verbose($"Restored {count} files");
            }
        }

        private void CopyModSourceCodeToSdk()
        {
            Report.Verbose("Copying mod source");
            DirectoryHelper.CopyDirectory(modInfo.SourceCodePath, edition.SdkSourceCodePath);
        }

        private void CleanSdkCompiledScripts()
        {
            Report.Verbose("Deleting SDK compiled scripts");
            DirectoryHelper.DeleteByExtension(edition.SdkXComGameCompiledScriptPath, SearchOption.AllDirectories, StringComparison.OrdinalIgnoreCase, ScriptExtension);
        }

        private void CleanModSdkCompiledScripts()
        {
            if (File.Exists(modSdkCompiledScriptPath))
            {
                Report.Verbose("Deleting mod compiled script");
                DirectoryHelper.Delete(modSdkCompiledScriptPath);
            }
        }

        private PackageFlags? GetBuiltStandardPackageFlags()
        {
            try
            {
                using var reader = new PackageReader(Path.Combine(edition.SdkXComGameCompiledScriptPath, KeyStandardPackageCompiledScriptFileName));
                var header = reader.ReadHeader();
                return header.PackageFlags;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void SmartCleanSdkCompiledScripts()
        {
            Report.Verbose("Smart-cleaning compiled scripts");
            foreach (var filePath in Directory.GetFiles(edition.SdkXComGameCompiledScriptPath))
            {
                var fileName = Path.GetFileName(filePath);
                if (!StandardCompiledScripts.Any(x => string.Equals(x, fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    Report.Verbose($"  Deleting non-standard compiled script file {fileName}", Verbosity.Loquacious);
                    DirectoryHelper.Delete(filePath);
                }
            }

            Report.Verbose("Smart-cleaning compiled script manifest", Verbosity.Loquacious);
            var manifestPath = Path.Combine(edition.SdkXComGameCompiledScriptPath, CompiledScriptManifestName);
            var lines = File.ReadAllLines(manifestPath).ToList();
            for (var i = 0; i < lines.Count; ++i)
            {
                var line = lines[i];
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var module = parts[2];
                    if (!StandardManifestModules.Any(x => string.Equals(x, module, StringComparison.OrdinalIgnoreCase)))
                    {
                        Report.Verbose($"  Removing line {line}", Verbosity.Periphrastic);
                        lines.RemoveAt(i);
                        --i;
                    }
                }
            }
            File.WriteAllLines(manifestPath, lines, Program.DefaultEncoding);
        }

        private void CompileGame()
        {
            ThrowIfCancelled();
            Report.Verbose("Compiling game");
            if (!compiler.CompileGame())
            {
                throw new Exception("Game script compilation failed (bad game source?)");
            }
            ThrowIfCancelled();
        }

        private void CompileMod()
        {
            ThrowIfCancelled();
            Report.Verbose("Compiling mod");
            if (!compiler.CompileMod(modInfo.ModName, modStagingPath))
            {
                throw new Exception("Mod compilation failed");
            }
            ThrowIfCancelled();
        }

        private void CompileShaders()
        {
            ThrowIfCancelled();
            Report.Verbose("Compiling shaders");
            if (!compiler.CompileShaders(modInfo.ModName))
            {
                throw new Exception("Shader compilation failed");
            }
            ThrowIfCancelled();
        }

        private bool IsDeployedShaderCacheUpToDate()
        {
            var upToDate = false;
            var shaderContent = modInfo.GetShaderContent();
            if (shaderContent.Any())
            {
                var contentDate =
                    shaderContent.Select(x => new FileInfo(x).LastWriteTime)
                                 .OrderByDescending(x => x)
                                 .ToArray()
                                 .First();
                Report.Verbose($"Shader content updated {contentDate:G}");

                //Report.Verbose($"Shader cache expected at {modShaderCacheInstallPath}");
                if (File.Exists(modShaderCacheInstallPath))
                {
                    var cacheDate = new FileInfo(modShaderCacheInstallPath).LastWriteTime;
                    Report.Verbose($"Shader cache updated {cacheDate:G}");
                    upToDate = cacheDate > contentDate;
                }
                else
                {
                    Report.Verbose("Shader cache not found");
                }
            }
            if (upToDate)
            {
                Report.Verbose("Shader cache is up to date");
            }
            else
            {
                Report.Verbose("Shader cache is out of date");
            }
            return upToDate;
        }

        private void CopyDeployedShaderCacheToStaging()
        {
            Report.Verbose("Copying deployed shader cache to staging");
            DirectoryHelper.CopyFile(modShaderCacheInstallPath, modShaderCacheStagingPath);
        }

        private void StageModCompiledScripts()
        {
            Report.Verbose("Copying compiled script");
            Directory.CreateDirectory(modStagingCompiledScriptFolderPath);
            File.Copy(modSdkCompiledScriptPath, modStagingCompiledScriptFilePath);
        }

        private void DeployMod()
        {
            Report.Verbose("Deploying mod");
            DirectoryHelper.Delete(modInstallPath);
            DirectoryHelper.CopyDirectory(modStagingPath, modInstallPath);
        }
    }
}
