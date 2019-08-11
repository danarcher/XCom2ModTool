using System;
using System.IO;

namespace XCom2ModTool
{
    internal class ModCreator
    {
        private static readonly string NewModTemplateFolder = "NewModTemplate";
        private static readonly string ModNamePlaceholder = "$MODNAME$";
        private static readonly string DescriptionPlaceholder = "$DESCRIPTION$";
        private static readonly string Guid1Placeholder = "$GUID1$";
        private static readonly string Guid2Placeholder = "$GUID2$";
        private static readonly string Guid3Placeholder = "$GUID3$";
        private static readonly string DefaultDescripton = string.Empty;

        public static void Create(string targetPath)
        {
            var modName = Path.GetFileName(targetPath);
            var description = DefaultDescripton;
            var guid1 = Guid.NewGuid().ToString("D");
            var guid2 = Guid.NewGuid().ToString("D");
            var guid3 = Guid.NewGuid().ToString("D");

            var sourcePath = Path.Combine(Program.HomePath, NewModTemplateFolder, ModNamePlaceholder);
            if (Directory.Exists(targetPath))
            {
                throw new Exception($"{modName} already exists");
            }

            DirectoryHelper.Copy(sourcePath, targetPath);

            DirectoryHelper.ReplaceDirectoryContents(
                targetPath, 
                extensions: null, 
                StringComparison.OrdinalIgnoreCase, 
                SearchOption.AllDirectories, 
                rename: true, 
                (ModNamePlaceholder, modName),
                (DescriptionPlaceholder, description),
                (Guid1Placeholder, guid1),
                (Guid2Placeholder, guid2),
                (Guid3Placeholder, guid3));
        }
    }
}