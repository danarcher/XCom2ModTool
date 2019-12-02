using System;
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
                Console.WriteLine("[Press any key to exit.]");
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

            // Parse verbosity.
            for (var i = 0; i < args.Count; ++i)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "--verbose":
                    case "-v":
                    case "/verbose":
                    case "/v":
                        Report.IsVerbose = true;
                        args.RemoveAt(i--);
                        break;
                    default:
                        break;
                }
            }

            Settings.Load();
            var edition = Settings.Default.Edition;

            // Parse other options.
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
                    case "--debug":
                    case "/debug":
                        Settings.Default.Debug = true;
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
                case "wotc":
                    SetEdition(args, XCom2.Wotc);
                    break;
                case "base":
                case "legacy":
                    SetEdition(args, XCom2.Base);
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
                    PackageInfo(args);
                    break;
                case "save-info":
                    SaveInfo(args);
                    break;
                default:
                    Report.Error($"{command} is not a {Name} command. See '{Name} --help'.");
                    Environment.ExitCode = 1;
                    break;
            }
        }

        private static void SetEdition(List<string> args, XCom2Edition edition)
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

        private static void PackageInfo(List<string> args)
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
                    Console.WriteLine(text);
                }
            }
        }

        private static void SaveInfo(List<string> args)
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
                    Console.WriteLine(text);
                }
            }
        }

        private static void Help(XCom2Edition edition)
        {
            var indent = new string(' ', Name.Length);
            Console.WriteLine($"usage: {Name} [--version ] [ -v | --verbose ]");
            Console.WriteLine($"       {indent} [options]");
            Console.WriteLine($"       {indent} <command> [<args>]");
            Console.WriteLine();
            Console.WriteLine($"Options vary by command; see '{Name} help <command>'.");
            Console.WriteLine();
            Console.WriteLine($"Currently working on {edition.DisplayName}.");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  help                  Display help on a command");
            Console.WriteLine($"  wotc | base | legacy  Switch between {XCom2.Wotc.DisplayName} and {XCom2.Base.DisplayName}");
            Console.WriteLine("  create                Create a mod");
            Console.WriteLine("  rename                Rename a mod");
            Console.WriteLine("  build                 Build a mod");
            Console.WriteLine("  open                  Open a specific XCOM folder");
            Console.WriteLine("  clip                  Copy a specific XCOM folder to the clipboard");
            Console.WriteLine("  update-project        Update a mod's project file");
            Console.WriteLine("  new-guid              Generate a new GUID for a mod");
            Console.WriteLine("  package-info          Display info on an Unreal package");
            Console.WriteLine("  save-info             Display info on a save file");
            Console.WriteLine();
            Paths();
        }

        private static void Help(List<string> args, XCom2Edition edition)
        {
            var command = args.Count > 0 ? args[0] : string.Empty;
            var help = command switch
            {
                "wotc" => HelpSetEdition,
                "base" => HelpSetEdition,
                "legacy" => HelpSetEdition,
                "create" => HelpCreate,
                "rename" => HelpRename,
                "build" => HelpBuild,
                "open" => () => HelpOpen(edition),
                "clip" => () => HelpClip(edition),
                "update-project" => HelpUpdateProject,
                "new-guid" => HelpNewGuid,
                "package-info" => HelpPackageInfo,
                "save-info" => HelpSaveInfo,
                _ => new Action(() => Help(edition)),
            };
            help();
            return;
        }

        private static void HelpSetEdition()
        {
            Console.WriteLine($"To switch between {XCom2.Wotc.DisplayName} and {XCom2.Base.DisplayName}:");
            Console.WriteLine($"{Name} wotc");
            Console.WriteLine($"{Name} base");
            Console.WriteLine($"{Name} legacy");
        }

        private static void HelpCreate()
        {
            Console.WriteLine("To create a mod:");
            Console.WriteLine($"{Name} create <folder>");
        }

        private static void HelpRename()
        {
            Console.WriteLine("To rename a mod:");
            Console.WriteLine($"{Name} rename <from folder> <to folder>");
        }

        private static void HelpFolderContext()
        {
            Console.WriteLine();
            Console.WriteLine("If no folder is specified, the current directory must be part of a mod.");
        }

        private static void HelpBuild()
        {
            Console.WriteLine("To build a mod:");
            Console.WriteLine($"{Name} [--debug] build [full | fast | smart] [<folder>]");
            Console.WriteLine();
            Console.WriteLine("To clean a mod's build:");
            Console.WriteLine($"{Name} build clean [<folder>]");
            HelpFolderContext();
        }

        private static void HelpUpdateProject()
        {
            Console.WriteLine("To update a mod's project:");
            Console.WriteLine($"{Name} update-project [<folder>]");
            Console.WriteLine();
            Console.WriteLine("This sets the project's included files to equal the set of existent files.");
            HelpFolderContext();
        }

        private static void HelpNewGuid()
        {
            Console.WriteLine("To generate a new GUID for a mod project:");
            Console.WriteLine($"{Name} new-guid [<folder>]");
            Console.WriteLine();
            Console.WriteLine("This updates the project with a new GUID for the mod.");
            HelpFolderContext();
        }

        private static void HelpPackageInfo()
        {
            Console.WriteLine("To display info on an Unreal package:");
            Console.WriteLine($"{Name} package-info <file>");
        }

        private static void HelpSaveInfo()
        {
            Console.WriteLine("To display info on a save file:");
            Console.WriteLine($"{Name} save-info <file>");
        }

        private static void HelpOpen(XCom2Edition edition)
        {
            Console.WriteLine("To open a specific XCOM 2 folder or program:");
            Console.WriteLine($"{Name} open <name>");
            Console.WriteLine();
            ListFolders(edition, "open");
        }

        private static void HelpClip(XCom2Edition edition)
        {
            Console.WriteLine("To copy a specific XCOM 2 path to the clipboard:");
            Console.WriteLine($"{Name} clip <name>");
            Console.WriteLine();
            ListFolders(edition, "clip");
        }

        private static void ListFolders(XCom2Edition edition, string command)
        {
            Console.WriteLine("Names:");
            var folders = XCom2Browser.GetFolders();
            var length = folders.Max(x => x.name.Length) + 2;
            foreach (var folder in folders)
            {
                var indent = new string(' ', length - folder.name.Length);
                Console.WriteLine($"  {folder.name}{indent}{folder.describe(edition)}");
                if (Report.IsVerbose)
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
                        Console.WriteLine();
                    }
                }
            }
            if (!Report.IsVerbose)
            {
                Console.WriteLine();
                Console.WriteLine($"Use '{Name} --verbose {command}' to see folder paths.");
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

            var vdfAssembly = Assembly.GetAssembly(typeof(Gameloop.Vdf.VdfConvert));
            var vdfVersion = vdfAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var vdfCopyright = vdfAssembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;

            Console.WriteLine($"{Name} version {version}");
            Console.WriteLine(copyright);
            Console.WriteLine("https://github.com/danarcher/xcom2modtool");
            Console.WriteLine("Licensed under the GPL v2.0 to comply with LZO");
            Console.WriteLine();

            Console.WriteLine($"Gameloop.Vdf {vdfVersion}");
            Console.WriteLine($"{vdfCopyright}");
            Console.WriteLine("Licensed under the MIT License");
            Console.WriteLine("https://github.com/shravan2x/Gameloop.Vdf");
            Console.WriteLine();

            Console.WriteLine($"LZO {Lzo.Version} {Lzo.VersionDate}");
            Console.WriteLine("Copyright © 1996 - 2017 Markus F.X.J.Oberhumer");
            Console.WriteLine("Licensed under the GPL v2.0");
            Console.WriteLine("http://www.oberhumer.com/opensource/lzo/");

            Console.WriteLine();
            Paths();
        }

        private static void Paths()
        {
            foreach (var edition in new[] { XCom2.Base, XCom2.Wotc })
            {
                Console.WriteLine($"{edition.DisplayName} is {(edition.IsInstalled ? edition.Path : "not found")}");
                Console.WriteLine($"{edition.SdkDisplayName} is {(edition.IsSdkInstalled ? edition.SdkPath : "not found")}");
            }
        }
    }
}
