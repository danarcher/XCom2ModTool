using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace XCom2ModTool
{
    internal static class Program
    {
        public static string Name { get; } = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

        public static string HomePath { get; } = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public static Encoding DefaultEncoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

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
                        Help();
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
                    case "create":
                        Create(args.Skip(1).ToArray());
                        return;
                    case "rename":
                        Rename(args.Skip(1).ToArray());
                        return;
                    case "build":
                        Build(args.Skip(1).ToArray());
                        return;
                    case "open":
                        Open(args.Skip(1).ToArray());
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

        private static void Build(string[] args)
        {
            if (args.Length > 0 && args[0] == "clean")
            {
                BuildClean(args.Skip(1).ToArray());
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

            ModInfo modInfo = null;
            if (!ModInfo.FindModForCurrentDirectory(out modInfo) && args.Length != 1)
            {
                HelpBuild();
                return;
            }
            else if (modInfo == null)
            {
                modInfo = new ModInfo(args[0]);
            }
            else
            {
                Report.Verbose($"[{modInfo.RootPath}]");
            }

            var builder = new ModBuilder(XCom2.Base, modInfo);
            builder.Build(buildType);
        }

        private static void BuildClean(string[] args)
        {
            if (args.Length != 1)
            {
                HelpBuild();
                return;
            }

            var builder = new ModBuilder(XCom2.Base, new ModInfo(args[0]));
            builder.Clean();
        }

        private static void Open(string[] args)
        {
            if (args.Length != 1)
            {
                HelpOpen();
                return;
            }

            XCom2Browser.Browse(args[0]);
        }

        private static void Help()
        {
            var indent = new string(' ', Name.Length);
            Console.WriteLine($"usage: {Name} [--version ] [ -h | --help ] [ -v | --verbose ]");
            Console.WriteLine($"       {indent} <command> [<args>]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  create         Create a mod");
            Console.WriteLine("  rename         Rename a mod");
            Console.WriteLine("  build          Build a mod");
            Console.WriteLine("  open           Open a specific XCOM folder");
            Console.WriteLine();
            Paths();
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
            Console.WriteLine($"{Name} build [full | fast | smart] [<folder>]");
            Console.WriteLine();
            Console.WriteLine("To clean a mod's build:");
            Console.WriteLine($"{Name} build clean [<folder>]");
            Console.WriteLine();
            Console.WriteLine("If no folder is specified, the current directory must be part of a mod.");
        }

        private static void HelpOpen()
        {
            Console.WriteLine("To open a specific XCOM 2 folder:");
            Console.WriteLine($"{Name} open <folder>");
            Console.WriteLine();
            Console.WriteLine("Folders:");
            var folders = XCom2Browser.GetFolders();
            var length = folders.Max(x => x.name.Length) + 2;
            foreach (var folder in folders)
            {
                var indent = new string(' ', length - folder.name.Length);
                Console.WriteLine($"  {folder.name}{indent}{folder.description}");
                if (Report.IsVerbose)
                {
                    indent = new string(' ', length);
                    string path;
                    try
                    {
                        path = folder.getPath();
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
                Console.WriteLine($"Use '{Name} --verbose open' to see folder paths.");
            }
        }

        private static void Version()
        {
            var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var copyright = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
            Console.WriteLine($"{Name} version {version}");
            Console.WriteLine(copyright);
            Console.WriteLine();
            Paths();
        }

        private static void Paths()
        {
            Console.WriteLine($"{XCom2.Base.DisplayName} is {(XCom2.Base.IsInstalled ? XCom2.Base.Path : "not found")}");
            Console.WriteLine($"{XCom2.Wotc.DisplayName} is {(XCom2.Wotc.IsInstalled ? XCom2.Wotc.Path : "not found")}");
            Console.WriteLine($"{XCom2.Base.SdkDisplayName} is {(XCom2.Base.IsSdkInstalled ? XCom2.Base.SdkPath : "not found")}");
            Console.WriteLine($"{XCom2.Wotc.SdkDisplayName} is {(XCom2.Wotc.IsSdkInstalled ? XCom2.Wotc.SdkPath : "not found")}");
        }
    }
}
