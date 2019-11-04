using Newtonsoft.Json;

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
        public PackageFlags PackageFlags { get; set; }

        public override string ToString()
        {
            return $"Version={Version}/{LicenseeVersion}, Group={PackageGroup}, Flags={PackageFlags.ToString().Replace(", ", "|")}";
        }
    }
}
