using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace XCom2ModTool
{
    internal class ModBuilder
    {
        private static readonly string ScriptFolderName = "Script";
        private static readonly string ScriptExtension = ".u";
        private static readonly string CompiledScriptManifestName = "Manifest.txt";
        private static readonly string KeyStandardPackageCompiledScriptFileName = "Core" + ScriptExtension;

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

        private bool modHasSourceCode;
        private bool modHasShaderContent;
        private string modStagingCompiledScriptFolderPath = null;
        private string modSdkCompiledScriptPath = null;
        private string modStagingCompiledScriptFilePath = null;

        public ModBuilder(XCom2Edition edition, ModInfo modInfo, CancellationToken cancellation)
        {
            this.edition = edition;
            this.modInfo = modInfo;
            this.cancellation = cancellation;
            compiler = new Compiler(edition);
            modStagingPath = edition.GetModStagingPath(modInfo);
            modInstallPath = edition.GetModInstallPath(modInfo);
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
            modProject = ModProject.Load(modInfo);

            if (!string.Equals(modProject.Title, modInfo.ModName, StringComparison.Ordinal))
            {
                Report.Warning($"Mod name {modInfo.ModName} does not match title '{modProject.Title}' in project {modInfo.ProjectName}");
            }

            CleanModStaging();
            ThrowIfCancelled();

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
                        if (Options.Debug != flags.Value.HasFlag(PackageFlags.Debug))
                        {
                            Options.Debug = !Options.Debug;
                            Report.Verbose($"Detected {(Options.Debug ? "debug" : "release")} build of {KeyStandardPackageCompiledScriptFileName}");
                        }
                        SmartCleanSdkSourceCode();
                        CopyModSourceCodeToSdk();
                        SmartCleanSdkCompiledScripts();
                        break;
                }

                CompileMod();
                if (modHasShaderContent)
                {
                    CompileShaders();
                }
                StageModCompiledScripts();
                SmartCleanSdkSourceCode();
            }

            DeployMod();
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
                DirectoryHelper.Copy(sourcePath, targetPath);
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
                if (!StandardSourceCodeFolderNames.Any(x => string.Equals(x, folderName, StringComparison.OrdinalIgnoreCase)))
                {
                    Report.Verbose($"  Deleting non-standard source {folderName}");
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
            var count = DirectoryHelper.Copy(edition.SdkOriginalSourceCodePath, edition.SdkSourceCodePath);
            Report.Verbose($"Restored {count} files");
        }

        private void CopyModSourceCodeToSdk()
        {
            Report.Verbose("Copying mod source");
            DirectoryHelper.Copy(modInfo.SourceCodePath, edition.SdkSourceCodePath);
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
                var info = new PackageInfo(Path.Combine(edition.SdkXComGameCompiledScriptPath, KeyStandardPackageCompiledScriptFileName));
                return info.IsValid ? info.Flags : (PackageFlags?)null;
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
                    Report.Verbose($"  Deleting non-standard compiled script file {fileName}");
                    DirectoryHelper.Delete(filePath);
                }
                else
                {
                    var info = new PackageInfo(filePath);
                    Report.Verbose($"{fileName} {info}");
                }
            }

            Report.Verbose("Smart-cleaning compiled script manifest");
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
                        Report.Verbose($"  Removing line {line}");
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
            DirectoryHelper.Copy(modStagingPath, modInstallPath);
        }
    }
}
