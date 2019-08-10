using System.IO;

namespace XCom2ModTool
{
    internal class ModInfo
    {
        private static readonly string SolutionExtension = ".XCOM_sln";
        private static readonly string SolutionOptionsExtension = ".v12.XCOM_suo";
        private static readonly string ProjectExtension = ".x2proj";
        public static readonly string MetadataExtension = ".XComMod";
        public static readonly string SourceCodeFolder = "Src";

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
    }
}
