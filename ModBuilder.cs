using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace XCom2ModTool
{
    internal class ModBuilder
    {
        private static string ConfigFolderName = "Config";
        private static string ContentFolderName = "Content";
        private static string LocalizationFolderName = "Localization";
        private static string ScriptFolderName = "Script";
        private static string ScriptExtension = ".u";

        private static string ProjectXmlProperties = "PropertyGroup";
        private static string ProjectXmlPropertySteamPublishId = "SteamPublishID";
        private static string ProjectXmlPropertyTitle = "Name";
        private static string ProjectXmlPropertyDescription = "Description";

        private XCom2Edition edition;
        private ModInfo modInfo;
        private string modStagingPath;
        private string modInstallPath;

        public ModBuilder(XCom2Edition edition, ModInfo modInfo)
        {
            this.edition = edition;
            this.modInfo = modInfo;
            modStagingPath = edition.GetModStagingPath(modInfo);
            modInstallPath = edition.GetModInstallPath(modInfo);
        }

        public void Clean()
        {
            if (Directory.Exists(modStagingPath))
            {
                Report.Verbose($"Cleaning staging folder");
                Directory.Delete(modStagingPath, true);
            }
            else
            {
                Report.Verbose($"Staging is clean");
            }
        }

        public void Build(bool full)
        {
            Report.Verbose($"Building {modInfo.ModName}");
            Clean();
            StageFolder(ConfigFolderName);
            StageFolder(ContentFolderName);
            StageFolder(LocalizationFolderName);

            var hasSourceCode = StageFolder(ModInfo.SourceCodeFolder);
            string modSourcePath = null;
            string modStagingScriptFolderPath = null;
            string modIntermediateCompiledScriptPath = null;
            string modStagingCompiledScriptPath = null;

            if (hasSourceCode)
            {
                modSourcePath = Path.Combine(modInfo.InnerPath, ModInfo.SourceCodeFolder);
                modStagingScriptFolderPath = Path.Combine(modStagingPath, ScriptFolderName);
                modIntermediateCompiledScriptPath = Path.Combine(edition.SdkXComGameScriptPath, modInfo.ModName + ScriptExtension);
                modStagingCompiledScriptPath = Path.Combine(modStagingScriptFolderPath, modInfo.ModName + ScriptExtension);
                Directory.CreateDirectory(modStagingScriptFolderPath);
            }

            Report.Verbose("Loading metadata");
            var projectContents = XDocument.Parse(File.ReadAllText(modInfo.ProjectPath));
            var projectProperties = projectContents.Root.Local(ProjectXmlProperties);
            var modSteamPublishId = projectProperties.Local(ProjectXmlPropertySteamPublishId).Value;
            var modTitle = projectProperties.Local(ProjectXmlPropertyTitle).Value;
            var modDescription = projectProperties.Local(ProjectXmlPropertyDescription).Value;

            Report.Verbose($"  Title: {modTitle}");
            Report.Verbose($"  Description: {modDescription}");
            Report.Verbose($"  Steam Publish ID: {modSteamPublishId}");

            if (!string.Equals(modTitle, modInfo.ModName, StringComparison.Ordinal))
            {
                Report.Warning($"Mod name {modInfo.ModName} does not match title '{modTitle}' in project {modInfo.ProjectName}");
            }

            Report.Verbose("Writing metadata");
            var metaFileName = modInfo.ModName + ModInfo.MetadataExtension;
            var metaPath = Path.Combine(modStagingPath, metaFileName);
            using (var writer = new StreamWriter(metaPath, append: false, Program.DefaultEncoding))
            {
                writer.WriteLine("[mod]");
                writer.WriteLine($"publishedFileId={modSteamPublishId}");
                writer.WriteLine($"Title={modTitle}");
                writer.WriteLine($"Description={modDescription}");
                // TODO: RequiresXPACK=true
            }

            if (hasSourceCode)
            {
                Report.Verbose("Cleaning SDK source");
                if (Directory.Exists(edition.SdkSourceCodePath))
                {
                    Directory.Delete(edition.SdkSourceCodePath, true);
                }

                Report.Verbose("Copying SDK source");
                var count = DirectoryHelper.Copy(edition.SdkOriginalSourceCodePath, edition.SdkSourceCodePath);
                Report.Verbose($"Copied {count} files");

                Report.Verbose("Copying mod source");
                DirectoryHelper.Copy(modSourcePath, edition.SdkSourceCodePath);

                if (full)
                {
                    Report.Verbose("Deleting SDK compiled scripts");
                    DirectoryHelper.DeleteByExtension(edition.SdkXComGameScriptPath, SearchOption.AllDirectories, StringComparison.OrdinalIgnoreCase, ScriptExtension);
                }
                else
                {
                    if (File.Exists(modIntermediateCompiledScriptPath))
                    {
                        Report.Verbose("Deleting mod compiled script");
                        File.Delete(modIntermediateCompiledScriptPath);
                    }
                }

                Report.Verbose("Compiling game");
                if (!Compile("make", "-nopause", "-unattended"))
                {
                    throw new Exception("Game script compilation failed (bad game source?)");
                }

                Report.Verbose("Compiling mod");
                if (!Compile("make", "-nopause", "-mods", modInfo.ModName, modStagingPath))
                {
                    throw new Exception("Mod compilation failed");
                }

                // TODO: build shaders if necessary.

                Report.Verbose("Copying compiled script");
                File.Copy(modIntermediateCompiledScriptPath, modStagingCompiledScriptPath);
            }

            Report.Verbose("Deploying mod");
            if (Directory.Exists(modInstallPath))
            {
                Directory.Delete(modInstallPath, true);
            }
            DirectoryHelper.Copy(modStagingPath, modInstallPath);
        }

        private bool StageFolder(string folderName)
        {
            var sourcePath = Path.Combine(modInfo.InnerPath, folderName);
            var targetPath = Path.Combine(modStagingPath, folderName);
            if (Directory.Exists(sourcePath))
            {
                Report.Verbose($"Staging {folderName}");
                DirectoryHelper.Copy(sourcePath, targetPath);
                return true;
            }
            return false;
        }

        private bool Compile(params string[] args)
        {
            for (var i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
                var containsQuotes = arg.IndexOf("\"", 0, StringComparison.Ordinal) > 0;
                var containsSpaces = arg.IndexOf(" ", 0, StringComparison.Ordinal) > 0;
                if (containsQuotes)
                {
                    arg = arg.Replace("\"", "\"\"");
                }
                if (containsQuotes || containsSpaces)
                {
                    arg = $"\"{arg}\"";
                }
                args[i] = arg;
            }

            var psi = new ProcessStartInfo();
            psi.FileName = edition.SdkCompilerPath;
            psi.Arguments = string.Join(" ", args);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            Report.Verbose($"> {psi.FileName} {psi.Arguments}");
            var alive = true;

            void FilterCompilerOutput(string text, TextWriter writer)
            {
                if (string.IsNullOrWhiteSpace(text) ||
                    (text.StartsWith("-----") && text.EndsWith("-----") && text.Contains(" - Release")) ||
                    text.Contains("Executing Class UnrealEd.MakeCommandlet") ||
                    text.Contains("invalid uniform expression set") ||
                    text.Contains("Execution of commandlet took") ||
                    text.Contains("No scripts need recompiling") ||
                    text.Contains("Analyzing..."))
                {
                    return;
                }

                var regex = new Regex(Regex.Escape("Scripts successfully compiled - saving package '") + "(.*)" + Regex.Escape("'"));
                var match = regex.Match(text);
                if (match.Success)
                {
                    var fileName = Path.GetFileName(match.Groups[1].Value);
                    text = $"{fileName} ok";
                    Report.Verbose(text);
                    return;
                }
                else
                {
                    regex = new Regex(Regex.Escape("Success - ") + "([0-9]+)" + Regex.Escape(" error(s), ") + "([0-9]+)" + Regex.Escape(" warning(s)"));
                    match = regex.Match(text);
                    if (match.Success)
                    {
                        var errors = int.Parse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture);
                        var warnings = int.Parse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture);
                        if (errors == 0 && warnings == 0)
                        {
                            return;
                        }
                        text = $"{errors} errors and {warnings} warnings";
                    }
                }
                writer.WriteLine(text);
            }

            var process = new Process();
            process.OutputDataReceived += (s, e) => FilterCompilerOutput(e.Data, Console.Out);
            process.ErrorDataReceived += (s, e) => FilterCompilerOutput(e.Data, Console.Error);
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => alive = false;
            process.StartInfo = psi;
            if (!process.Start())
            {
                throw new Exception("Could not start the compiler");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while (alive)
            {
                Thread.Sleep(50);
            }

            var exitCode = process.ExitCode;
            process.Dispose();

            return exitCode == 0;
        }
    }
}
