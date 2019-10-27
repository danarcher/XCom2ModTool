using Newtonsoft.Json;

namespace XCom2ModTool.UnrealPackages
{
    internal class CompressedChunk
    {
        [JsonConverter(typeof(HexJsonConverter))]
        public uint Signature { get; set; }
        public uint BlockSize { get; set; }
        public uint CompressedSize { get; set; }
        public uint UncompressedSize { get; set; }
        public CompressedBlock[] Blocks { get; set; }
    }
}
