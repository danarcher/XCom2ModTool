using System;
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
        public static string Name { get; } = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

        public static string HomePath { get; } = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public static Encoding DefaultEncoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Run(args);
            }
            catch (Exception ex)
            {
                Report.Exception(ex);
                Environment.ExitCode = 1;
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("[Press any key to exit.]");
                Console.ReadKey();
            }
        }

        private static void Run(string[] args)
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cancellation.Cancel();
            };

            var edition = XCom2.Base;

            while (args.Length > 0)
            {
                switch (args[0])
                {
                    case "help":
                    case "--help":
                    case "-h":
                    case "-?":
                    case "/help":
                    case "/h":
                    case "/?":
                        Help(args.Skip(1).ToArray(), edition);
                        return;
                    case "version":
                    case "--version":
                    case "/version":
                        Version();
                        return;
                    case "--verbose":
                    case "-v":
                    case "/verbose":
                    case "/v":
                        Report.IsVerbose = true;
                        args = args.Skip(1).ToArray();
                        break;
                    case "--debug":
                    case "/debug":
                        Options.Debug = true;
                        args = args.Skip(1).ToArray();
                        break;
                    case "--wotc":
                    case "/wotc":
                    case "-w":
                    case "/w":
                        edition = XCom2.Wotc;
                        args = args.Skip(1).ToArray();
                        break;
                    case "create":
                        Create(args.Skip(1).ToArray());
                        return;
                    case "rename":
                        Rename(args.Skip(1).ToArray());
                        return;
                    case "build":
                        Build(args.Skip(1).ToArray(), edition, cancellation.Token);
                        return;
                    case "open":
                        Open(args.Skip(1).ToArray(), edition);
                        return;
                    case "clip":
                        Clip(args.Skip(1).ToArray(), edition);
                        return;
                    case "update-project":
                        UpdateProject(args.Skip(1).ToArray(), edition);
                        return;
                    case "new-guid":
                        NewGuid(args.Skip(1).ToArray(), edition);
                        return;
                    case "package-info":
                        PackageInfo(args.Skip(1).ToArray());
                        return;
                    case "save-info":
                        SaveInfo(args.Skip(1).ToArray());
                        return;
                    default:
                        Report.Error($"{args[0]} is not a {Name} command. See '{Name} --help'.");
                        Environment.ExitCode = 1;
                        return;
                }
            }

            Help();
        }

        private static void Create(string[] args)
        {
            if (args.Length != 1)
            {
                HelpCreate();
                return;
            }

            ModCreator.Create(args[0]);
        }

        private static void Rename(string[] args)
        {
            if (args.Length != 2)
            {
                HelpRename();
                return;
            }

            ModRenamer.Rename(args[0], args[1]);
        }

        private static void Build(string[] args, XCom2Edition edition, CancellationToken cancellation)
        {
            if (args.Length > 0 && args[0] == "clean")
            {
                BuildClean(args.Skip(1).ToArray(), edition, cancellation);
                return;
            }

            var buildType = ModBuildType.Smart;
            while (args.Length > 0)
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
                args = args.Skip(1).ToArray();
            }

            if (!LocateMod(ref args, out ModInfo modInfo) || args.Length > 0)
            {
                HelpBuild();
                return;
            }

            var builder = new ModBuilder(modInfo, edition, cancellation);
            builder.Build(buildType);
        }

        private static void BuildClean(string[] args, XCom2Edition edition, CancellationToken cancellation)
        {
            if (!LocateMod(ref args, out ModInfo modInfo) || args.Length > 0)
            {
                HelpBuild();
                return;
            }

            var builder = new ModBuilder(modInfo, edition, cancellation);
            builder.Clean();
        }

        private static void Open(string[] args, XCom2Edition edition)
        {
            if (args.Length != 1)
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

        private static void Clip(string[] args, XCom2Edition edition)
        {
            if (args.Length != 1)
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

        private static void UpdateProject(string[] args, XCom2Edition edition)
        {
            if (!LocateMod(ref args, out ModInfo modInfo) || args.Length > 0)
            {
                HelpUpdateProject();
                return;
            }

            var project = ModProject.Load(modInfo, edition);
            project.Update();
            project.Save(modInfo.ProjectPath);
        }

        private static void NewGuid(string[] args, XCom2Edition edition)
        {
            if (!LocateMod(ref args, out ModInfo modInfo) || args.Length > 0)
            {
                HelpNewGuid();
                return;
            }

            var project = ModProject.Load(modInfo, edition);
            project.Id = Guid.NewGuid();
            project.Save(modInfo.ProjectPath);
        }

        private static void PackageInfo(string[] args)
        {
            if (args.Length != 1)
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

        private static void SaveInfo(string[] args)
        {
            if (args.Length != 1)
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

        private static void Help()
        {
            var indent = new string(' ', Name.Length);
            Console.WriteLine($"usage: {Name} [--version ] [-w | --wotc] [ -v | --verbose ]");
            Console.WriteLine($"       {indent} [options]");
            Console.WriteLine($"       {indent} <command> [<args>]");
            Console.WriteLine();
            Console.WriteLine($"Options vary by command; see '{Name} help <command>'.");
            Console.WriteLine();
            Console.WriteLine($"Specify -w or --wotc for {XCom2.Wotc.DisplayName}.");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  help           Display help on a command");
            Console.WriteLine("  create         Create a mod");
            Console.WriteLine("  rename         Rename a mod");
            Console.WriteLine("  build          Build a mod");
            Console.WriteLine("  open           Open a specific XCOM folder");
            Console.WriteLine("  clip           Copy a specific XCOM folder to the clipboard");
            Console.WriteLine("  update-project Update a mod's project file");
            Console.WriteLine("  new-guid       Generate a new GUID for a mod");
            Console.WriteLine("  package-info   Display info on an Unreal package");
            Console.WriteLine("  save-info  Display info on a save file");
            Console.WriteLine();
            Paths();
        }

        private static void Help(string[] args, XCom2Edition edition)
        {
            var command = args.Length > 0 ? args[0] : string.Empty;
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
                _ => (Action)Help,
            };
            help();
            return;
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

        private static void HelpBuild()
        {
            Console.WriteLine("To build a mod:");
            Console.WriteLine($"{Name} [--debug] build [full | fast | smart] [<folder>]");
            Console.WriteLine();
            Console.WriteLine("To clean a mod's build:");
            Console.WriteLine($"{Name} build clean [<folder>]");
            Console.WriteLine();
            Console.WriteLine("If no folder is specified, the current directory must be part of a mod.");
        }

        private static void HelpUpdateProject()
        {
            Console.WriteLine("To update a mod's project:");
            Console.WriteLine($"{Name} update-project [<folder>]");
            Console.WriteLine();
            Console.WriteLine("This sets the project's included files to equal the set of existent files.");
            Console.WriteLine();
            Console.WriteLine("If no folder is specified, the current directory must be part of a mod.");
        }

        private static void HelpNewGuid()
        {
            Console.WriteLine("To generate a new GUID for a mod project:");
            Console.WriteLine($"{Name} new-guid [<folder>]");
            Console.WriteLine();
            Console.WriteLine("This updates the project with a new GUID for the mod.");
            Console.WriteLine();
            Console.WriteLine("If no folder is specified, the current directory must be part of a mod.");
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
                    Console.WriteLine($"  {indent}{path}");
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

        private static bool LocateMod(ref string[] args, out ModInfo modInfo)
        {
            if (!ModInfo.FindModForCurrentDirectory(out modInfo))
            {
                if (args.Length < 1)
                {
                    return false;
                }
                modInfo = new ModInfo(args[0]);
                args = args.Skip(1).ToArray();
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
