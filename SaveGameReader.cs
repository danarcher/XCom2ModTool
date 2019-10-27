using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using XCom2ModTool.UnrealPackages;

namespace XCom2ModTool
{
    internal class SaveGameReader : BinaryDataReader
    {
        public SaveGameReader(string path) : base(path) { }

        public SaveGame ReadToEnd()
        {
            var save = new SaveGame();

            // Samples (dev debug only):
            // v20: %USERPROFILE%\Documents\My Games\XCOM2\XComGame\SaveData\save62
            // v21: %USERPROFILE%\Documents\My Games\XCOM2 War of the Chosen\XComGame\SaveData\save_IRONMAN- Campaign 3
            // v22: %USERPROFILE%\Documents\My Games\XCOM2 War of the Chosen\XComGame\SaveData\save_WOTC Debug Full Campaign No Chosen

            static string Part(string[] parts, int index) => parts.Length > index ? parts[index] : string.Empty;

            save.Version = U32();
            if (save.Version < SaveGame.MinSupportedVersion || save.Version > SaveGame.MaxSupportedVersion)
            {
                throw new InvalidDataException("Invalid save file, or unsupport version");
            }

            var headerByteCount = (int)U32();
            save.HeaderSizeCheck = (uint)headerByteCount;
            save.Checksum = U32();
            using (Detour())
            {
                var header = Bytes(headerByteCount);
                // Zero the CRC, since we haven't computed it yet.
                for (var i = 8; i < 12; ++i)
                {
                    header[i] = 0;
                }
                save.ChecksumCheck = BZip2Crc.Compute(header);
            }
            save.UncompressedSize = U32();
            save.CampaignNumber = U32();
            save.SaveSlotNumber = U32();

            var description = FString().Split('\n');
            save.Description = new SaveGame.ParsedDescription
            {
                SaveDateTime = ParseDateTime24(Part(description, 0), Part(description, 1)),
                SaveName = Part(description, 2),
                MissionTypeName = Part(description, 3),
            };

            var dateTime = FString().Split('\n');
            save.SaveDateTime = ParseDateTime24(Part(dateTime, 0), Part(dateTime, 1));

            save.MapCommand = FString();
            save.Tactical = Bool();
            if (save.Tactical)
            {
                save.Description.OperationName = Part(description, 4);
                save.Description.GameDateTime = ParseDateTimeUtc12(Part(description, 5), Part(description, 6));
                save.Description.MapName = Part(description, 7);
            }
            else
            {
                save.Description.GameDateTime = ParseDateTimeUtc12(Part(description, 4), Part(description, 5));
            }
            save.Ironman = Bool();
            save.AutoSave = Bool();
            save.QuickSave = Bool();
            save.Language = FString();
            save.Unknown6 = U32();
            save.Unknown7 = U32();
            save.ArchiveFileVersion = U32();
            save.ArchiveLicenseeVersion = U32();
            save.CampaignStartDateTime = FString();
            save.MissionImageUri = FString();
            save.PlayerSaveName = FString();

            var nameCount = U32();
            save.DLCPackNames = new string[nameCount];
            for (var i = 0; i < save.DLCPackNames.Length; ++i)
            {
                save.DLCPackNames[i] = FString();
            }

            var friendlyNameCount = U32();
            save.DLCPackFriendlyNames = new string[friendlyNameCount];
            for (var i = 0; i < save.DLCPackFriendlyNames.Length; ++i)
            {
                save.DLCPackFriendlyNames[i] = FString();
            }

            if (save.Version >= 21)
            {
                save.Mission = U32();
                save.Month = U32();
                save.Turn = U32();
                save.Action = U32();
                save.MissionType = FString();
                save.DebugSave = Bool();
                save.PreMission = Bool();
                save.PostMission = Bool();
            }
            if (save.Version >= 22)
            {
                save.Ladder = Bool();
            }

            // If we've read all of the header, we should be at the UPK signature 0x9e2a83c1 (bytes 0xc1, 0x83, 0x2a, 0x9e)
            var signature = U32();
            if (signature != PackageSignature.Valid)
            {
                throw new InvalidDataException("Expected package signature to follow the save game header");
            }
            Position -= sizeof(uint);

            var chunks = new List<CompressedChunk>();
            while (!EndOfStream)
            {
                var packageReader = new PackageReader(Reader, leaveOpen: true);
                var chunk = packageReader.ReadCompressedChunk();
                chunks.Add(chunk);
            }
            save.Chunks = chunks.ToArray();

            return save;
        }

        private static DateTime? ParseDateTime24(string dateText, string timeText)
        {
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
    }
}
