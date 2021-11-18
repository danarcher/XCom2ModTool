﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using XCom2ModTool.UnrealPackages;

namespace XCom2ModTool
{
    internal static class Program
    {
        public static string ProductName { get; } = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product;

        public static string Name { get; } = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

        public static string HomePath { get; } = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public static Encoding DefaultEncoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

        [STAThread]
        public static void Main(string[] args)
        {
            Settings.Load();

            try
            {
                Run(args.ToList());
            }
            catch (Exception ex)
            {
                Report.Exception(ex);
                Environment.ExitCode = 1;
            }

            Settings.Save();

            if (Debugger.IsAttached)
            {
                Report.WriteLine("[Press any key to exit.]");
                Console.ReadKey();
            }
        }

        private static void Run(List<string> args)
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cancellation.Cancel();
            };

            var edition = Settings.Default.Editions[Settings.Default.Edition];

            // Parse options.
            for (var i = 0; i < args.Count; ++i)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "--help":
                    case "-h":
                    case "-?":
                    case "/help":
                    case "/h":
                    case "/?":
                        Help(new List<string>(), edition);
                        return;
                    case "--version":
                    case "/version":
                        Version();
                        return;
                    case "--verbose":
                    case "-v":
                    case "/verbose":
                    case "/v":
                        Report.Verbosity = Verbosity.Verbose;
                        args.RemoveAt(i--);
                        break;
                    case "-vv":
                    case "/vv":
                        Report.Verbosity = Verbosity.Loquacious;
                        args.RemoveAt(i--);
                        break;
                    case "-vvv":
                    case "/vvv":
                        Report.Verbosity = Verbosity.Periphrastic;
                        args.RemoveAt(i--);
                        break;
                    case "--debug":
                    case "/debug":
                        Settings.Default.Debug = true;
                        args.RemoveAt(i--);
                        break;
                    case "--highlander":
                    case "/highlander":
                        Settings.Default.Highlander = true;
                        args.RemoveAt(i--);
                        break;
                    default:
                        if (arg.StartsWith("-") || arg.StartsWith("/"))
                        {
                            Report.Error($"{arg} is not a {Name} option. See '{Name} --help'.");
                            Environment.ExitCode = 1;
                            return;
                        }
                        break;
                }
            }

            // Parse commands.
            if (args.Count == 0)
            {
                Help(edition);
                return;
            }

            var command = args[0];
            args.RemoveAt(0);
            switch (command)
            {
                case "help":
                    Help(args, edition);
                    break;
                case "version":
                    Version();
                    break;
                case "create":
                    Create(args);
                    break;
                case "rename":
                    Rename(args);
                    break;
                case "build":
                    Build(args, edition, cancellation.Token);
                    break;
                case "open":
                    Open(args, edition);
                    break;
                case "clip":
                    Clip(args, edition);
                    break;
                case "update-project":
                    UpdateProject(args, edition);
                    break;
                case "new-guid":
                    NewGuid(args, edition);
                    break;
                case "package-info":
                    PackageInfo(args, cancellation.Token);
                    break;
                case "save-info":
                    SaveInfo(args, cancellation.Token);
                    break;
                default:
                    if (!TrySetEdition(args, command)) {
                        Report.Error($"{command} is not a {Name} command. See '{Name} --help'.");
                        Environment.ExitCode = 1;
                    }                    
                    break;
            }
        }

        private static bool TrySetEdition(List<string> args, string query) {
            try {
                string editionInternalName = Settings.Default.Shortnames[query];
                XCom2Edition edition = Settings.Default.Editions[editionInternalName];
                SetEdition(args, editionInternalName);
                return true;
            } catch {}

            return false;
        }

        private static void SetEdition(List<string> args, string edition)
        {
            if (args.Count != 0)
            {
                HelpSetEdition();
                return;
            }

            Settings.Default.Edition = edition;
        }

        private static void Create(List<string> args)
        {
            if (args.Count != 1)
            {
                HelpCreate();
                return;
            }

            ModCreator.Create(args[0]);
        }

        private static void Rename(List<string> args)
        {
            if (args.Count != 2)
            {
                HelpRename();
                return;
            }

            ModRenamer.Rename(args[0], args[1]);
        }

        private static void Build(List<string> args, XCom2Edition edition, CancellationToken cancellation)
        {
            if (args.Count > 0 && args[0] == "clean")
            {
                args.RemoveAt(0);
                BuildClean(args, edition, cancellation);
                return;
            }

            var buildType = ModBuildType.Smart;
            while (args.Count > 0)
            {
                if (args[0] == "full")
                {
                    buildType = ModBuildType.Full;
                }
                else if (args[0] == "fast")
                {
                    buildType = ModBuildType.Fast;
                }
                else if (args[0] == "smart")
                {
                    buildType = ModBuildType.Smart;
                }
                else
                {
                    break;
                }
                args.RemoveAt(0);
            }

            if (!LocateMod(args, out ModInfo modInfo) || args.Count > 0)
            {
                HelpBuild();
                return;
            }

            var builder = new ModBuilder(modInfo, edition, cancellation);
            builder.Build(buildType);
        }

        private static void BuildClean(List<string> args, XCom2Edition edition, CancellationToken cancellation)
        {
            if (!LocateMod(args, out ModInfo modInfo) || args.Count > 0)
            {
                HelpBuild();
                return;
            }

            var builder = new ModBuilder(modInfo, edition, cancellation);
            builder.Clean();
        }

        private static void Open(List<string> args, XCom2Edition edition)
        {
            if (args.Count != 1)
            {
                HelpOpen(edition);
                return;
            }

            var folder = args[0];
            try
            {
                XCom2Browser.Browse(folder, edition);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new InvalidOperationException($"'{folder}' is not a recognized folder. See '{Name} help open'.");
            }
        }

        private static void Clip(List<string> args, XCom2Edition edition)
        {
            if (args.Count != 1)
            {
                HelpClip(edition);
                return;
            }

            var folder = args[0];
            try
            {
                XCom2Browser.CopyToClipboard(folder, edition);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new InvalidOperationException($"'{folder}' is not a recognized folder. See '{Name} help clip'.");
            }
        }

        private static void UpdateProject(List<string> args, XCom2Edition edition)
        {
            if (!LocateMod(args, out ModInfo modInfo) || args.Count > 0)
            {
                HelpUpdateProject();
                return;
            }

            var project = ModProject.Load(modInfo, edition);
            project.Update();
            project.Save(modInfo.ProjectPath);
        }

        private static void NewGuid(List<string> args, XCom2Edition edition)
        {
            if (!LocateMod(args, out ModInfo modInfo) || args.Count > 0)
            {
                HelpNewGuid();
                return;
            }

            var project = ModProject.Load(modInfo, edition);
            project.Id = Guid.NewGuid();
            project.Save(modInfo.ProjectPath);
        }

        private static void PackageInfo(List<string> args, CancellationToken cancellation)
        {
            if (args.Count != 1)
            {
                HelpPackageInfo();
                return;
            }

            var path = args[0];
            using var reader = new PackageReader(path);
            var header = reader.ReadHeader();

            var json = header.ToJson();
            using (var jsonReader = new StringReader(json))
            {
                while (true)
                {
                    var text = jsonReader.ReadLine();
                    if (text == null)
                    {
                        break;
                    }
                    Report.WriteLine(text);
                    cancellation.ThrowIfCancellationRequested();
                }
            }
        }

        private static void SaveInfo(List<string> args, CancellationToken cancellation)
        {
            if (args.Count != 1)
            {
                HelpSaveInfo();
                return;
            }

            var path = args[0];
            using var reader = new SaveGameReader(path);
            var saveGame = reader.ReadToEnd();

            var json = saveGame.ToJson();
            using (var jsonReader = new StringReader(json))
            {
                while (true)
                {
                    var text = jsonReader.ReadLine();
                    if (text == null)
                    {
                        break;
                    }
                    Report.WriteLine(text);
                    cancellation.ThrowIfCancellationRequested();
                }
            }
        }

        private static void Help(XCom2Edition edition)
        {
            var indent = new string(' ', Name.Length);
            Report.WriteLine($"usage: <green>{Name}</green> [--version ] [ -v | --verbose ]");
            Report.WriteLine($"       {indent} [options]");
            Report.WriteLine($"       {indent} \\<command> [\\<args>]");
            Report.WriteLine();
            Report.WriteLine($"Options vary by command; see '{Name} help \\<command>'.");
            Report.WriteLine();
            Report.WriteLine($"Currently working on {edition.DisplayName}.");
            Report.WriteLine();
            Report.WriteLine("Commands:");
            Report.WriteLine("  <yellow>help</yellow>                  Display help on a command");
            var first = true;

            foreach (KeyValuePair<string, string> shortname in Settings.Default.Shortnames) {
                if (first) {
                    Report.Write(" ");
                    first = false;
                } else {
                    Report.Write(" |");
                }

                Report.Write($" <yellow>{shortname.Key}</yellow>");
            }
            Report.WriteLine($"  Switch between game editions");
            Report.WriteLine("  <yellow>create</yellow>                Create a mod");
            Report.WriteLine("  <yellow>rename</yellow>                Rename a mod");
            Report.WriteLine("  <yellow>build</yellow>                 Build a mod");
            Report.WriteLine("  <yellow>open</yellow>                  Open a specific game folder");
            Report.WriteLine("  <yellow>clip</yellow>                  Copy a specific game folder to the clipboard");
            Report.WriteLine("  <yellow>update-project</yellow>        Update a mod's project file");
            Report.WriteLine("  <yellow>new-guid</yellow>              Generate a new GUID for a mod");
            Report.WriteLine("  <yellow>package-info</yellow>          Display info on an Unreal package");
            Report.WriteLine("  <yellow>save-info</yellow>             Display info on a save file");
            Report.WriteLine();
            Paths();
        }

        private static void Help(List<string> args, XCom2Edition edition)
        {
            var command = args.Count > 0 ? args[0] : string.Empty;
            var help = command switch
            {
                "create" => HelpCreate,
                "rename" => HelpRename,
                "build" => HelpBuild,
                "open" => () => HelpOpen(edition),
                "clip" => () => HelpClip(edition),
                "update-project" => HelpUpdateProject,
                "new-guid" => HelpNewGuid,
                "package-info" => HelpPackageInfo,
                "save-info" => HelpSaveInfo,
                _ => HasEditionShortname(command) ? HelpSetEdition : new Action(() => Help(edition)),
            };
            help();
            return;
        }

        private static void HelpSetEdition()
        {
            Report.WriteLine($"To switch between editions:");
            foreach (KeyValuePair<string, string> shortname in Settings.Default.Shortnames) {
                Report.WriteLine($"{Name} {shortname.Key}");
            }
        }

        private static bool HasEditionShortname(string command)
        {
            return Settings.Default.Shortnames.ContainsKey(command);
        }

        private static void HelpCreate()
        {
            Report.WriteLine("To create a mod:");
            Report.WriteLine($"{Name} create \\<folder>");
        }

        private static void HelpRename()
        {
            Report.WriteLine("To rename a mod:");
            Report.WriteLine($"{Name} rename \\<from folder> \\<to folder>");
        }

        private static void HelpFolderContext()
        {
            Report.WriteLine();
            Report.WriteLine("If no folder is specified, the current directory must be part of a mod.");
        }

        private static void HelpBuild()
        {
            Report.WriteLine("To build a mod:");
            Report.WriteLine($"{Name} [--debug] [--highlander] build [full | fast | smart] [\\<folder>]");
            Report.WriteLine();
            Report.WriteLine("Options:");
            Report.WriteLine("  <yellow>--debug</yellow>       Make this a debug build rather than release");
            Report.WriteLine("  <yellow>--highlander</yellow>  Build this mod against the community highlander");
            Report.WriteLine();
            Report.WriteLine("Build Types:");
            Report.WriteLine("  <yellow>full</yellow>          Build game scripts, mod scripts and mod shaders");
            Report.WriteLine("  <yellow>fast</yellow>          Build mod scripts and mod shaders");
            Report.WriteLine("  <yellow>smart</yellow>         Minimise unnecessary work for a faster build");
            Report.WriteLine();
            Report.WriteLine("The default is a smart, release build without the highlander.");
            Report.WriteLine();
            Report.WriteLine("To clean a mod's build:");
            Report.WriteLine($"{Name} build clean [\\<folder>]");
            HelpFolderContext();
        }

        private static void HelpUpdateProject()
        {
            Report.WriteLine("To update a mod's project:");
            Report.WriteLine($"{Name} update-project [\\<folder>]");
            Report.WriteLine();
            Report.WriteLine("This sets the project's included files to equal the set of existent files.");
            HelpFolderContext();
        }

        private static void HelpNewGuid()
        {
            Report.WriteLine("To generate a new GUID for a mod project:");
            Report.WriteLine($"{Name} new-guid [\\<folder>]");
            Report.WriteLine();
            Report.WriteLine("This updates the project with a new GUID for the mod.");
            HelpFolderContext();
        }

        private static void HelpPackageInfo()
        {
            Report.WriteLine("To display info on an Unreal package:");
            Report.WriteLine($"{Name} package-info \\<file>");
        }

        private static void HelpSaveInfo()
        {
            Report.WriteLine("To display info on a save file:");
            Report.WriteLine($"{Name} save-info \\<file>");
        }

        private static void HelpOpen(XCom2Edition edition)
        {
            Report.WriteLine("To open a specific XCOM 2 folder or program:");
            Report.WriteLine($"{Name} open \\<name>");
            Report.WriteLine();
            ListFolders(edition, "open");
        }

        private static void HelpClip(XCom2Edition edition)
        {
            Report.WriteLine("To copy a specific XCOM 2 path to the clipboard:");
            Report.WriteLine($"{Name} clip \\<name>");
            Report.WriteLine();
            ListFolders(edition, "clip");
        }

        private static void ListFolders(XCom2Edition edition, string command)
        {
            Report.WriteLine("Names:");
            var folders = XCom2Browser.GetFolders();
            var length = folders.Max(x => x.name.Length) + 2;
            foreach (var folder in folders)
            {
                var indent = new string(' ', length - folder.name.Length);
                Report.WriteLine($"  {folder.name}{indent}{folder.describe(edition)}");
                if (Report.Verbosity >= Verbosity.Verbose)
                {
                    indent = new string(' ', length);
                    string path;
                    try
                    {
                        path = folder.getPath(edition);
                    }
                    catch (Exception ex)
                    {
                        path = $"{ex.Message}";
                    }
                    Report.Verbose($"  {indent}{path}");
                    if (folder != folders.Last())
                    {
                        Report.WriteLine();
                    }
                }
            }
            if (Report.Verbosity < Verbosity.Verbose)
            {
                Report.WriteLine();
                Report.WriteLine($"Use '{Name} --verbose {command}' to see folder paths.");
            }
        }

        private static bool LocateMod(List<string> args, out ModInfo modInfo)
        {
            if (!ModInfo.FindModForCurrentDirectory(out modInfo))
            {
                if (args.Count < 1)
                {
                    return false;
                }
                modInfo = new ModInfo(args[0]);
                args.RemoveAt(0);
            }
            else
            {
                Report.Verbose($"[{modInfo.RootPath}]");
            }
            return true;
        }

        private static void Version()
        {
            var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var copyright = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;

            Report.WriteLine($"<green>{ProductName}</green> version {version}");
            Report.WriteLine(copyright);
            Report.WriteLine("<cyan>https://github.com/danarcher/xcom2modtool</cyan>");
            Report.WriteLine("Licensed under the GPL v2.0 to comply with LZO");
            Report.WriteLine();

            Report.WriteLine($"<darkgreen>LZO</darkgreen> {Lzo.Version} {Lzo.VersionDate}");
            Report.WriteLine("Copyright © 1996 - 2017 Markus F.X.J.Oberhumer");
            Report.WriteLine("Licensed under the GPL v2.0");
            Report.WriteLine("<cyan>http://www.oberhumer.com/opensource/lzo/</cyan>");

            Report.WriteLine();
            Paths();
        }

        private static void Paths()
        {
            foreach (var edition in Settings.Default.Editions.Values)
            {
                Report.WriteLine($"{edition.DisplayName} is {(edition.IsInstalled ? edition.Path : "not found")}");
                Report.WriteLine($"{edition.SdkDisplayName} is {(edition.IsSdkInstalled ? edition.SdkPath : "not found")}");
            }
        }
    }
}
