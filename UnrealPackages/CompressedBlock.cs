using Newtonsoft.Json;

namespace XCom2ModTool.UnrealPackages
{
    internal class CompressedBlock
    {
        public uint CompressedSize { get; set; }
        public uint UncompressedSize { get; set; }

        [JsonConverter(typeof(AbbreviatedByteArrayJsonConverter))]
        public byte[] Data { get; set; }
    }
}
