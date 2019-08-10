using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XCom2ModTool
{
    internal static class ModRenamer
    {
        public static void Rename(string sourcePath, string targetPath)
        {
            // Work out where everything is, and should be.
            var sourceInfo = new ModInfo(sourcePath);
            var targetInfo = new ModInfo(targetPath);

            // Move the root folder.
            Report.Verbose("Moving root folder");
            Directory.Move(sourceInfo.RootPath, targetInfo.RootPath);
            sourceInfo.RootFolder = targetInfo.RootFolder;
            sourceInfo.RootPath = targetInfo.RootPath;

            // Move the inner folder.
            Report.Verbose("Moving inner folder");
            Directory.Move(sourceInfo.InnerPath, targetInfo.InnerPath);
            sourceInfo.InnerFolder = targetInfo.InnerFolder;

            // Move the solution.
            Report.Verbose("Moving solution");
            File.Move(sourceInfo.SolutionPath, targetInfo.SolutionPath);
            sourceInfo.SolutionName = targetInfo.SolutionName;

            // Try to move solution options.
            try
            {
                Report.Verbose("Moving solution options");
                File.Move(sourceInfo.SolutionOptionsPath, targetInfo.SolutionOptionsPath);
                sourceInfo.SolutionOptionsName = targetInfo.SolutionOptionsName;
            }
            catch (Exception ex) when (ex is IOException || ex is DirectoryNotFoundException)
            {
                Report.Warning($"could not move solution options file: {ex.Message}");
            }

            // Move the project.
            Report.Verbose("Moving project");
            File.Move(sourceInfo.ProjectPath, targetInfo.ProjectPath);
            sourceInfo.ProjectName = targetInfo.ProjectName;

            // Move the source code.
            if (Directory.Exists(sourceInfo.SourceCodeInnerPath))
            {
                Directory.Move(sourceInfo.SourceCodeInnerPath, targetInfo.SourceCodeInnerPath);
                sourceInfo.SourceCodeInnerFolder = targetInfo.SourceCodeInnerFolder;
            }

            // Fix the solution.
            Report.Verbose($"Fixing {sourceInfo.SolutionName}");
            var solutionText = File.ReadAllText(sourceInfo.SolutionPath);
            solutionText = solutionText.Replace($"\"{sourceInfo.ModName}", $"\"{targetInfo.ModName}")
                                       .Replace($"\\{sourceInfo.ModName}", $"\\{targetInfo.ModName}");
            File.WriteAllText(sourceInfo.SolutionPath, solutionText, Program.DefaultEncoding);

            // Fix the project.
            Report.Verbose($"Fixing {sourceInfo.ProjectName}");
            var projectText = File.ReadAllText(sourceInfo.ProjectPath);
            projectText = projectText.Replace($">{sourceInfo.ModName}<", $">{targetInfo.ModName}<")
                                     .Replace($"\"{ModInfo.SourceCodeFolder}\\{sourceInfo.ModName}", $"\"{ModInfo.SourceCodeFolder}\\{targetInfo.ModName}");
            File.WriteAllText(sourceInfo.ProjectPath, projectText, Program.DefaultEncoding);

            // Find all configuration and localization files.
            Report.Verbose("Looking for configuration files");
            var configs = Directory.GetFiles(sourceInfo.InnerPath, "*.*", SearchOption.AllDirectories)
                                   .Select(x => new FileInfo(x))
                                   .Where(x => string.Equals(x.Extension, ".ini", StringComparison.Ordinal) ||
                                               string.Equals(x.Extension, ".int", StringComparison.Ordinal))
                                   .ToArray();
            Report.Verbose($"Found {configs.Length} .ini/.int files");

            // Fix references to the mod.
            var fixedConfigs = new HashSet<string>();
            var fixedLines = 0;
            foreach (var config in configs)
            {
                Report.Verbose($"Processing {config.Name}");
                var lines = File.ReadAllLines(config.FullName);
                for (var i = 0; i < lines.Length; ++i)
                {
                    var oldLine = lines[i];
                    var newLine = oldLine.Replace($"[{sourceInfo.ModName}.", $"[{targetInfo.ModName}.")
                                         .Replace($"DLCIdentifier=\"{sourceInfo.ModName}\"", $"DLCIdentifier=\"{targetInfo.ModName}\"")
                                         .Replace($"NonNativePackages={sourceInfo.ModName}", $"NonNativePackages={targetInfo.ModName}")
                                         .Replace($"ModPackages={sourceInfo.ModName}", $"ModPackages={targetInfo.ModName}");
                    if (!string.Equals(newLine, oldLine, StringComparison.Ordinal))
                    {
                        Report.Verbose($"fixed line {i + 1}: {newLine}");
                        lines[i] = newLine;

                        fixedConfigs.Add(config.FullName);
                        ++fixedLines;
                    }
                }
                File.WriteAllLines(config.FullName, lines, Program.DefaultEncoding);
            }
        }
    }
}
