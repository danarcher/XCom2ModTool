using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

        public bool CompileGame() => Compile("make", "-nopause", "-unattended");

        public bool CompileMod(string modName, string stagingPath) => Compile("make", "-nopause", "-mods", modName, stagingPath);

        private bool Compile(params string[] args)
        {
            var start = new ProcessStartInfo
            {
                FileName = edition.SdkCompilerPath,
                Arguments = EscapeAndJoinArguments(args),
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
            process.Dispose();

            return exitCode == 0;
        }

        private static string EscapeAndJoinArguments(string[] args)
        {
            var newArgs = new string[args.Length];
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
                newArgs[i] = arg;
            }
            return string.Join(" ", newArgs);
        }

        private static void FilterOutput(string text, TextWriter writer, HashSet<string> duplicates)
        {
            if (string.IsNullOrWhiteSpace(text?.Trim('-')) ||
                (text.StartsWith("-----") && text.EndsWith("-----") && text.Contains(" - Release")) ||
                text.Contains("Executing Class UnrealEd.MakeCommandlet") ||
                text.Contains("invalid uniform expression set") ||
                text.Contains("Execution of commandlet took") ||
                text.Contains("No scripts need recompiling") ||
                text.Contains("Analyzing...") ||
                text.Contains("Compile aborted due to errors") ||
                text.Contains("Warning/Error Summary"))
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
                    // Don't duplicate errors or warnings.
                    regex = new Regex(Regex.Escape(" : ") + "(Error|Warning)" + Regex.Escape(", "));
                    match = regex.Match(text);
                    if (match.Success)
                    {
                        if (duplicates.Contains(text))
                        {
                            return;
                        }
                        duplicates.Add(text);
                    }
                }
            }
            writer.WriteLine(text);
        }
    }
}
