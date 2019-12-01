using Newtonsoft.Json;

namespace XCom2ModTool.UnrealPackages
{
    [JsonConverter(typeof(StringJsonConverter))]
    internal class PackageImport : PackageReferenceable
    {
        public string PackageName { get; set; }
        public string TypeName { get; set; }
        public ObjectReference Owner { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            if (Owner?.To != null)
            {
                return Owner.To.ToString() + "." + Name;
            }
            return Name;
        }
    }
}
