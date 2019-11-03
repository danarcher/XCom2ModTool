using System.IO;

namespace XCom2ModTool
{
    internal class ModMetadata
    {
        public static readonly string Extension = ".XComMod";

        public ModMetadata(ModProject project)
        {
            Title = project.Title;
            Description = project.Description;
            SteamPublishId = project.SteamPublishId;
            RequiresExpansion = project.Edition.IsExpansion;
        }

        public string Title { get; set; }
        public string Description { get; set; }
        public ulong SteamPublishId { get; set; }
        public bool RequiresExpansion { get; set; }

        public void Save(string path)
        {
            using (var writer = new StreamWriter(path, append: false, Program.DefaultEncoding))
            {
                writer.WriteLine("[mod]");
                writer.WriteLine($"publishedFileId={SteamPublishId}");
                writer.WriteLine($"Title={Title}");
                writer.WriteLine($"Description={Description}");
                if (RequiresExpansion)
                {
                    writer.WriteLine("RequiresXPACK=true");
                }
            }
        }

        public static void Save(ModProject project, string path) => new ModMetadata(project).Save(path);
    }
}
