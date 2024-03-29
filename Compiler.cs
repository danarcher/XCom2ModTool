﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;

namespace XCom2ModTool
{
    internal class Compiler
    {
        private readonly XCom2Edition edition;

        public Compiler(XCom2Edition edition)
        {
            this.edition = edition;
        }

        public Dictionary<string, string> ReplacePaths = new Dictionary<string, string>();

        public bool CompileGame() => Compile("make", "-debug", "-nopause", "-unattended", "-verbose");

        public bool CompileMod(string modName, string stagingPath) => Compile("make", "-debug", "-nopause", "-mods", modName, stagingPath);

        public bool CompileShaders(string modName) => Compile("precompileshaders", "-nopause", "platform=pc_sm4", $"DLC={modName}");

        private bool Compile(params string[] args)
        {
            if (!Settings.Default.Debug)
            {
                args = args.Except(new[] { "-debug" }).ToArray();
            }

            var start = new ProcessStartInfo
            {
                FileName = edition.SdkCompilerPath,
                Arguments = PathHelper.EscapeAndJoinArguments(args),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Report.Verbose($"> {start.FileName} {start.Arguments}");

            var process = new Process
            {
                StartInfo = start,
                EnableRaisingEvents = true,
            };

            var duplicates = new HashSet<string>(StringComparer.Ordinal);
            process.OutputDataReceived += (s, e) => FilterOutput(e.Data, Console.Out, duplicates);
            process.ErrorDataReceived += (s, e) => FilterOutput(e.Data, Console.Error, duplicates);

            if (!process.Start())
            {
                throw new Exception("Could not start the compiler");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while (!process.HasExited)
            {
                Thread.Sleep(50);
            }

            var exitCode = process.ExitCode;
            Report.Verbose($"Compiler process exit code {exitCode}, HasExited={process.HasExited}");
            process.Dispose();

            return exitCode == 0;
        }

        private void FilterOutput(string text, TextWriter writer, HashSet<string> duplicates)
        {
            if (text == null)
            {
                return;
            }

            if (Report.Verbosity < Verbosity.Periphrastic)
            {
                if (string.IsNullOrWhiteSpace(text?.Trim('-')) ||
                    (text.StartsWith("-----") && text.EndsWith("-----") && (text.Contains(" - Release") || text.Contains(" - Debug"))) ||
                    text.Contains("Executing Class UnrealEd.MakeCommandlet") ||
                    text.Contains("Executing Class UnrealEd.PrecompileShadersCommandlet") ||
                    text.Contains("invalid uniform expression set") ||
                    text.Contains("Execution of commandlet took") ||
                    text.Contains("No scripts need recompiling") ||
                    text.Contains("Analyzing...") ||
                    text.Contains("Compile aborted due to errors") ||
                    text.Contains("Warning/Error Summary") ||
                    text.Contains("Compiling shaders for") ||
                    text.Contains("Starting package") ||
                    text.Contains("Package processing complete") ||
                    (text.Contains(" = ") && text.Contains(" min")) ||
                    text.Contains("NumFullyLoadedPackages") ||
                    text.Contains("NumFastPackages") ||
                    text.Contains("Warning, INI file contains an incorrect case") ||
                    text.Contains("LoadAnIniFile was unable to find FilenameToLoad:") ||
                    text.StartsWith("Parsing ") ||
                    text.StartsWith("Compiling ") ||
                    text.StartsWith("Importing Defaults for") ||
                    text.StartsWith("Exporting native class declarations for") ||
                    text.StartsWith("... deferring"))
                {
                    // None of these lines are interesting.
                    return;
                }

                // Cut down long success messages.
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
                    // Cut down error/warning count messages.
                    regex = new Regex("(Success|Failure)" + Regex.Escape(" - ") + "([0-9]+)" + Regex.Escape(" error(s), ") + "([0-9]+)" + Regex.Escape(" warning(s)"));
                    match = regex.Match(text);
                    if (match.Success)
                    {
                        var errors = int.Parse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture);
                        var warnings = int.Parse(match.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture);
                        if (errors == 0 && warnings == 0)
                        {
                            return;
                        }
                        text = $"{errors} errors and {warnings} warnings";
                    }
                    else
                    {
                        // Look for errors/warnings.
                        regex = new Regex(Regex.Escape(" : ") + "(Error|Warning)" + Regex.Escape(", "));
                        match = regex.Match(text);
                        if (match.Success)
                        {
                            if (duplicates.Contains(text))
                            {
                                // Don't duplicate errors/warnings.
                                return;
                            }
                            duplicates.Add(text);

                            // Replace paths in error/warning messages.
                            foreach (var item in ReplacePaths)
                            {
                                text = Regex.Replace(text, Regex.Escape(item.Key), item.Value.Replace("$", "$$"), RegexOptions.IgnoreCase);
                            }
                        }
                    }
                }
            }

            text = SecurityElement.Escape(text);
            if (text.IndexOf("error", StringComparison.InvariantCultureIgnoreCase) >= 0 && !text.Contains("0 errors") && !text.Contains("0 error(s)"))
            {
                text = $"<error>{text}</error>";
            }
            else if (text.IndexOf("warning", StringComparison.InvariantCultureIgnoreCase) >= 0 && !text.Contains("0 warnings") && !text.Contains("0 warning(s)"))
            {
                text = $"<warning>{text}</warning>";
            }

            Report.WriteXmlLine(writer, text);
        }
    }
}
