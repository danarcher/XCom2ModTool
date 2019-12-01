using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace XCom2ModTool.UnrealPackages
{
    internal class PackageHeader
    {
        public bool IsDebug => PackageFlags.HasFlag(PackageFlags.Debug);

        [JsonConverter(typeof(HexJsonConverter))]
        public uint Signature { get; set; }
        public ushort Version { get; set; }
        public ushort LicenseeVersion { get; set; }
        public uint HeaderSize { get; set; }
        public string PackageGroup { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public PackageFlags PackageFlags { get; set; }
        [JsonIgnore]
        public GlobalName[] Names { get; set; }
        public PackageExport[] Exports { get; set; }
        public PackageImport[] Imports { get; set; }

        public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public override string ToString()
        {
            return $"Version={Version}/{LicenseeVersion}, Group={PackageGroup}, Flags={PackageFlags.ToString().Replace(", ", "|")}";
        }
    }
}
