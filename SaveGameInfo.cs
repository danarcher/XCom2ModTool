using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace XCom2ModTool
{
    internal class SaveGameInfo
    {
        public SaveGameInfo(string path)
        {
            // Samples (dev debug only):
            // v20: %USERPROFILE%\Documents\My Games\XCOM2\XComGame\SaveData\save62
            // v21: %USERPROFILE%\Documents\My Games\XCOM2 War of the Chosen\XComGame\SaveData\save_IRONMAN- Campaign 3
            // v22: %USERPROFILE%\Documents\My Games\XCOM2 War of the Chosen\XComGame\SaveData\save_WOTC Debug Full Campaign No Chosen

            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                uint U32() => reader.ReadUInt32();
                string FString() => PackageInfo.ReadFString(reader);
                bool Bool() => reader.ReadUInt32() != 0;
                string Part(string[] parts, int index) => parts.Length > index ? parts[index] : string.Empty;

                Version = U32();
                var headerByteCount = (int)U32();
                HeaderSizeCheck = (uint)headerByteCount;
                Checksum = U32();
                using (var crcReader = new BinaryReader(File.OpenRead(path)))
                {
                    var headerBytes = crcReader.ReadBytes(headerByteCount);
                    // Zero the CRC, since we haven't computed it yet.
                    headerBytes[8] = 0; headerBytes[9] = 0; headerBytes[10] = 0; headerBytes[11] = 0;
                    ChecksumCheck = BZip2Crc.Compute(headerBytes);
                }
                UncompressedSize = U32();
                CampaignNumber = U32();
                SaveSlotNumber = U32();

                var description = FString().Split('\n');
                Description = new SaveDescription
                {
                    SaveDateTime = ParseDateTime24(Part(description, 0), Part(description, 1)),
                    SaveName = Part(description, 2),
                    MissionTypeName = Part(description, 3),
                };

                var dateTime = FString().Split('\n');
                SaveDateTime = ParseDateTime24(Part(dateTime, 0), Part(dateTime, 1));

                MapCommand = FString();
                Tactical = Bool();
                if (Tactical)
                {
                    Description.OperationName = Part(description, 4);
                    Description.GameDateTime = ParseDateTimeUtc12(Part(description, 5), Part(description, 6));
                    Description.MapName = Part(description, 7);
                }
                else
                {
                    Description.GameDateTime = ParseDateTimeUtc12(Part(description, 4), Part(description, 5));
                }
                Ironman = Bool();
                AutoSave = Bool();
                QuickSave = Bool();
                Language = FString();
                Unknown6 = U32();
                Unknown7 = U32();
                ArchiveFileVersion = U32();
                ArchiveLicenseeVersion = U32(); 
                CampaignStartDateTime = FString();
                MissionImageUri = FString();
                PlayerSaveName = FString();

                var nameCount = U32();
                DLCPackNames = new string[nameCount];
                for (var i = 0; i < DLCPackNames.Length; ++i)
                {
                    DLCPackNames[i] = FString();
                }

                var friendlyNameCount = U32();
                DLCPackFriendlyNames = new string[friendlyNameCount];
                for (var i = 0; i < DLCPackFriendlyNames.Length; ++i)
                {
                    DLCPackFriendlyNames[i] = FString();
                }

                if (Version >= 21)
                {
                    Mission = U32();
                    Month = U32();
                    Turn = U32();
                    Action = U32();
                    MissionType = FString();
                    DebugSave = Bool();
                    PreMission = Bool();
                    PostMission = Bool();
                }
                if (Version >= 22)
                {
                    Ladder = Bool();
                }

                // If we've read all of the header, this should be the UPK signature 0x9e2a83c1 (bytes 0xc1, 0x83, 0x2a, 0x9e)
                UpkCheck = U32();
            }
        }

        private static DateTime? ParseDateTime24(string dateText, string timeText)
        {
            Console.WriteLine($"{dateText} {timeText}");
            if (!DateTime.TryParseExact($"{dateText} {timeText}", "M/d/yyyy H:mm", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal | DateTimeStyles.NoCurrentDateDefault, out DateTime dateTime))
            {
                return null;
            }
            return dateTime;
        }

        private static DateTime? ParseDateTimeUtc12(string dateText, string timeText)
        {
            if (!DateTime.TryParseExact($"{dateText} {timeText}", "M/d/yyyy h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.NoCurrentDateDefault, out DateTime dateTime))
            {
                return null;
            }
            return dateTime;
        }

        public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public uint Version { get; set; }
        public uint HeaderSizeCheck { get; set; }
        [JsonConverter(typeof(HexStringJsonConverter))]
        public uint Checksum { get; set; }
        [JsonConverter(typeof(HexStringJsonConverter)), JsonProperty("$ChecksumCheck")]
        public uint ChecksumCheck { get; set; }
        public uint UncompressedSize { get; set; }
        public uint CampaignNumber { get; set; }
        public uint SaveSlotNumber { get; set; }
        public SaveDescription Description { get; set; } = new SaveDescription();
        public DateTime? SaveDateTime { get; set; }
        public string MapCommand { get; set; }
        public bool Tactical { get; set; }
        public bool Ironman { get; set; }
        public bool AutoSave { get; set; }
        public bool QuickSave { get; set; }
        public string Language { get; set; }
        public uint Unknown6 { get; set; }
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
        [JsonConverter(typeof(HexStringJsonConverter))]
        public uint UpkCheck { get; set; }

        public class SaveDescription
        {
            public DateTime? SaveDateTime { get; set; }
            public string SaveName { get; set; }
            public string MissionTypeName { get; set; }
            public string OperationName { get; set; }
            public DateTime? GameDateTime { get; set; }
            public string MapName { get; set; }
        }

        private class HexStringJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(uint).Equals(objectType);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue($"{value:X}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var hex = reader.ReadAsString();
                if (!hex.StartsWith("0x"))
                {
                    hex = "0x" + hex;
                }
                return Convert.ToUInt32(hex);
            }
        }
    }
}
