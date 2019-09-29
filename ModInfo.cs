using System;
using System.IO;
using System.Linq;

namespace XCom2ModTool
{
    internal class ModInfo
    {
        public static readonly string SolutionExtension = ".XCOM_sln";
        public static readonly string SolutionOptionsExtension = ".v12.XCOM_suo";
        public static readonly string ProjectExtension = ".x2proj";
        public static readonly string SourceCodeExtension = ".uc";
        public static readonly string PackageExtension = ".upk";
        public static readonly string MapExtension = ".umap";
        public static readonly string SourceCodeFolder = "Src";
        public static readonly string ContentFolder = "Content";
        public static readonly string ConfigFolder = "Config";
        public static readonly string LocalizationFolder = "Localization";

        public ModInfo(string path)
        {
            RootPath = DirectoryHelper.GetExactPathName(path);

            RootFolder = Path.GetFileName(RootPath);
            ModName = RootFolder;
            InnerFolder = ModName;
            SolutionName = ModName;
            SolutionOptionsName = ModName;
            ProjectName = ModName;
            SourceCodeInnerFolder = ModName;
        }

        // MyMod from C:\Mods\MYMOD
        public string RootFolder { get; set; }

        // C:\Mods\MyMod
        public string RootPath { get; set; }

        // MyMod
        public string ModName { get; set; }

        // MyMod from C:\Mods\MyMod\MYMOD
        public string InnerFolder { get; set; }

        // MyMod from C:\Mods\MyMod\MYMOD.XCOM_sln
        public string SolutionName { get; set; }

        // MyMod from C:\Mods\MyMod\MYMOD.v12.XCOM_suo
        public string SolutionOptionsName { get; set; }

        // MyMod from C:\Mods\MyMod\MyMod\MYMOD.x2proj
        public string ProjectName { get; set; }

        // MyMod from C:\Mods\MyMod\MyMod\Src\MYMOD
        public string SourceCodeInnerFolder { get; set; }

        // C:\Mods\MyMod\MyMod
        public string InnerPath => Path.Combine(RootPath, InnerFolder);

        // C:\Mods\MyMod\MyMod.XCOM_sln
        public string SolutionPath => Path.Combine(RootPath, SolutionName + SolutionExtension);
        
        // C:\Mods\MyMod\MyMod.v12.XCOM_suo
        public string SolutionOptionsPath => Path.Combine(RootPath, SolutionOptionsName + SolutionOptionsExtension);

        // C:\Mods\MyMod\MyMod\MyMod.x2proj
        public string ProjectPath => Path.Combine(InnerPath, ProjectName + ProjectExtension);

        // C:\Mods\MyMod\MyMod\Src
        public string SourceCodePath => Path.Combine(InnerPath, SourceCodeFolder);

        // C:\Mods\MyMod\MyMod\Src\MyMod
        public string SourceCodeInnerPath => Path.Combine(SourceCodePath, SourceCodeInnerFolder);

        // C:\Mods\MyMod\MyMod\Content
        public string ContentPath => Path.Combine(InnerPath, ContentFolder);

        // C:\Mods\MyMod\MyMod\Config
        public string ConfigPath => Path.Combine(InnerPath, ConfigFolder);

        // C:\Mods\MyMod\MyMod\Localization
        public string LocalizationPath => Path.Combine(InnerPath, LocalizationFolder);

        public bool HasSourceCode()
        {
            return Directory.Exists(SourceCodeInnerPath) &&
                   Directory.EnumerateFiles(SourceCodeInnerPath, "*" + SourceCodeExtension, SearchOption.AllDirectories).Any();
        }

        public bool HasShaderContent()
        {
            return Directory.Exists(ContentPath) && 
                   Directory.EnumerateFiles(ContentPath, "*.*", SearchOption.AllDirectories)
                            .Select(x => Path.GetExtension(x))
                            .Any(x => string.Equals(x, PackageExtension, StringComparison.OrdinalIgnoreCase) || 
                                      string.Equals(x, MapExtension, StringComparison.OrdinalIgnoreCase));
        }

        public static bool FindModForCurrentDirectory(out ModInfo modInfo)
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory.Parent != null)
            {
                var potentialModName = directory.Name;
                var potentialModPath = directory.FullName;
                var solutionPath = Path.Combine(potentialModPath, potentialModName + SolutionExtension);
                if (File.Exists(solutionPath))
                {
                    modInfo = new ModInfo(potentialModPath);
                    return true;
                }
                directory = directory.Parent;
            }

            modInfo = null;
            return false;
        }
    }
}
