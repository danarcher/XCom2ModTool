using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace XCom2ModTool
{
    internal class ModProject
    {
        private static readonly string XmlProject = "Project";
        private static readonly string XmlToolsVersion = "ToolsVersion";
        private static readonly string XmlDefaultTargets = "DefaultTargets";
        private static readonly string XmlPropertyGroup = "PropertyGroup";
        private static readonly string XmlGuid = "Guid";
        private static readonly string XmlTitle = "Name";
        private static readonly string XmlDescription = "Description";
        private static readonly string XmlSteamPublishId = "SteamPublishID";
        private static readonly string XmlAssemblyName = "AssemblyName";
        private static readonly string XmlRootNamespace = "RootNamespace";

        private static readonly string XmlItemGroup = "ItemGroup";
        private static readonly string XmlFolder = "Folder";
        private static readonly string XmlContent = "Content";
        private static readonly string XmlInclude = "Include";

        private static readonly string XmlImport = "Import";

        private static readonly string ToolsVersion = "12";
        private static readonly string DefaultTargets = "Default";
        private static readonly string MSBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";
        private static readonly string ImportProject = "$(MSBuildLocalExtensionPath)\\XCOM2.targets";

        private ModProject()
        {
        }

        public static ModProject Load(ModInfo modInfo)
        {
            if (PathHelper.IsRelative(modInfo.ProjectPath))
            {
                throw new InvalidOperationException($"The project path is a relative path");
            }

            var document = XDocument.Parse(File.ReadAllText(modInfo.ProjectPath));
            var properties = document.Root.GetElementsByLocalName(XmlPropertyGroup).First();

            var folderPath = Path.GetDirectoryName(modInfo.ProjectPath);
            var itemGroups = document.Root.GetElementsByLocalName(XmlItemGroup);

            var folders = itemGroups.SelectMany(x => x.GetElementsByLocalName(XmlFolder))
                                    .Select(x => x.GetAttributeByLocalName(XmlInclude).Value)
                                    .Select(x => PathHelper.MakeAbsolute(x, folderPath))
                                    .Select(x => DirectoryHelper.GetExactPathName(x));

            var content = itemGroups.SelectMany(x => x.GetElementsByLocalName(XmlContent))
                                    .Select(x => x.GetAttributeByLocalName(XmlInclude).Value)
                                    .Select(x => PathHelper.MakeAbsolute(x, folderPath))
                                    .Select(x => DirectoryHelper.GetExactPathName(x));

            var project = new ModProject
            {
                ModInfo = modInfo,
                Id = Guid.Parse(properties.GetElementByLocalName(XmlGuid).Value),
                Title = properties.GetElementByLocalName(XmlTitle).Value,
                Description = properties.GetElementByLocalName(XmlDescription).Value,
                AssemblyName = properties.GetElementByLocalName(XmlAssemblyName).Value,
                RootNamespace = properties.GetElementByLocalName(XmlRootNamespace).Value,
                SteamPublishId = ulong.Parse(properties.GetElementByLocalName(XmlSteamPublishId).Value, NumberStyles.None, CultureInfo.InvariantCulture),
                Folders = folders.ToArray(),
                Content = content.ToArray()
            };

            return project;
        }

        public ModInfo ModInfo { get; private set; }
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ulong SteamPublishId { get; set; }
        public string AssemblyName { get; set; }
        public string RootNamespace { get; set; }
        public string[] Folders { get; set; }
        public string[] Content { get; set; }

        public void Update()
        {
            var folderPath = Path.GetDirectoryName(ModInfo.ProjectPath);

            Content = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                               .Where(x => !x.EndsWith(ModInfo.SolutionExtension, StringComparison.OrdinalIgnoreCase) &&
                                           !x.EndsWith(ModInfo.ProjectExtension, StringComparison.OrdinalIgnoreCase) &&
                                           !x.EndsWith(ModInfo.SolutionOptionsExtension, StringComparison.OrdinalIgnoreCase))
                               .Select(x => DirectoryHelper.GetExactPathName(x))
                               .OrderBy(x => x)
                               .ToArray();

            Folders = Content.Select(x => Path.GetDirectoryName(x))
                             .Distinct()
                             .Where(x => !string.Equals(x, folderPath, StringComparison.OrdinalIgnoreCase))
                             .OrderBy(x => x)
                             .ToArray();
        }

        public void Save(string projectPath)
        {
            var folderPath = Path.GetDirectoryName(ModInfo.ProjectPath);

            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(XmlProject,
                    new XAttribute(XmlToolsVersion, ToolsVersion),
                    new XAttribute(XmlDefaultTargets, DefaultTargets),
                    new XElement(XmlPropertyGroup,
                        new XElement(XmlGuid, Id.ToString("D")),
                        new XElement(XmlTitle, Title),
                        new XElement(XmlDescription, Description),
                        new XElement(XmlSteamPublishId, SteamPublishId),
                        new XElement(XmlAssemblyName, AssemblyName),
                        new XElement(XmlRootNamespace, RootNamespace)),
                    new XElement(XmlItemGroup,
                        Folders.Select(x =>
                            new XElement(XmlFolder,
                                new XAttribute(XmlInclude, PathHelper.MakeRelative(x, folderPath)))).ToArray()),
                    new XElement(XmlItemGroup,
                        Content.Select(x =>
                            new XElement(XmlContent,
                                new XAttribute(XmlInclude, PathHelper.MakeRelative(x, folderPath)))).ToArray()),
                    new XElement(XmlImport,
                        new XAttribute(XmlProject, ImportProject))));

            document.Root.SetDefaultXmlNamespace(MSBuildNamespace);
            document.Save(ModInfo.ProjectPath);
        }
    }
}
