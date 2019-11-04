using System;
using Newtonsoft.Json;
using XCom2ModTool.UnrealPackages;

namespace XCom2ModTool
{
    internal class SaveGame
    {
        public static readonly int MinSupportedVersion = 20;
        public static readonly int MaxSupportedVersion = 22;

        public uint Version { get; set; }

        [JsonConverter(typeof(HexJsonConverter)), JsonProperty("$HeaderSizeCheck")]
        public uint HeaderSizeCheck { get; set; }

        [JsonConverter(typeof(HexJsonConverter))]
        public uint Checksum { get; set; }

        [JsonConverter(typeof(HexJsonConverter)), JsonProperty("$ChecksumCheck")]
        public uint ChecksumCheck { get; set; }

        public uint UncompressedSize { get; set; }
        public uint CampaignNumber { get; set; }
        public uint SaveSlotNumber { get; set; }
        public ParsedDescription Description { get; set; }
        public DateTime? SaveDateTime { get; set; }
        public string MapCommand { get; set; }
        public bool Tactical { get; set; }
        public bool Ironman { get; set; }
        public bool AutoSave { get; set; }
        public bool QuickSave { get; set; }
        public string Language { get; set; }
        [JsonConverter(typeof(HexJsonConverter))]
        public uint Unknown6 { get; set; }
        [JsonConverter(typeof(HexJsonConverter))]
        public uint Unknown7 { get; set; }
        public uint ArchiveFileVersion { get; set; }
        public uint ArchiveLicenseeVersion { get; set; }
        public string CampaignStartDateTime { get; set; }
        public string MissionImageUri { get; set; }
        public string PlayerSaveName { get; set; }
        public string[] DLCPackNames { get; set; }
        public string[] DLCPackFriendlyNames { get; set; }
        public uint Mission { get; set; }
        public uint Month { get; set; }
        public uint Turn { get; set; }
        public uint Action { get; set; }
        public string MissionType { get; set; }
        public bool DebugSave { get; set; }
        public bool PreMission { get; set; }
        public bool PostMission { get; set; }
        public bool Ladder { get; set; }

        [JsonIgnore]
        public CompressedChunk[] Chunks { get; set; }

        [JsonIgnore]
        public byte[] AllChunksData { get; set; }

        [JsonIgnore]
        public ParsedNameTable NameTable { get; set; }

        public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public class ParsedDescription
        {
            public DateTime? SaveDateTime { get; set; }
            public string SaveName { get; set; }
            public string MissionTypeName { get; set; }
            public string OperationName { get; set; }
            public DateTime? GameDateTime { get; set; }
            public string MapName { get; set; }
        }

        public class ParsedNameTable
        {
            public uint Version { get; set; }
            public uint LicenseeVersion { get; set; }
            public ParsedNameEntry[] Names { get; set; }
        }

        public class ParsedNameEntry
        {
            public string Name { get; set; }

            [JsonConverter(typeof(HexJsonConverter))]
            public uint Unknown1 { get; set; }

            [JsonConverter(typeof(HexJsonConverter))]
            public uint Unknown2 { get; set; }

            [JsonConverter(typeof(HexJsonConverter))]
            public uint Unknown3 { get; set; }

            [JsonConverter(typeof(HexJsonConverter))]
            public uint Unknown4 { get; set; }
        }
    }
}
