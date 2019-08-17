using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace XCom2ModTool
{
    internal class ModProject
    {
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

        private ModProject()
        {
        }

        public static ModProject Load(string projectPath)
        {
            if (PathHelper.IsRelative(projectPath))
            {
                throw new ArgumentException($"{nameof(projectPath)} is a relative path");
            }

            var document = XDocument.Parse(File.ReadAllText(projectPath));
            var properties = document.Root.GetElementsByLocalName(XmlPropertyGroup).Single();

            var folderPath = Path.GetDirectoryName(projectPath);
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

        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ulong SteamPublishId { get; set; }
        public string AssemblyName { get; set; }
        public string RootNamespace { get; set; }
        public string[] Folders { get; set; }
        public string[] Content { get; set; }
    }
}
