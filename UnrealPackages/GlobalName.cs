using Newtonsoft.Json;

namespace XCom2ModTool.UnrealPackages
{
    internal class GlobalName
    {
        public string Name { get; set; }
        [JsonConverter(typeof(HexJsonConverter))]
        public ulong Flags { get; set; }
    }
}