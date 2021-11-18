using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace XCom2ModTool
{
    internal class Settings
    {
        private static readonly string Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Program.ProductName, "settings.json");

        public static Settings Default { get; private set; }

        [JsonIgnore]
        public bool Debug { get; set; }

        [JsonIgnore]
        public bool Highlander { get; set; }

        public bool UseColoredOutput { get; set; } = true;

        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;

        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor WarningColor { get; set; } = ConsoleColor.Yellow;

        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor InfoColor { get; set; } = ConsoleColor.Gray;

        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor VerboseColor { get; set; } = ConsoleColor.Gray;

        [JsonConverter(typeof(EditionDictJsonConverter))]
        public Dictionary<string, XCom2Edition> Editions { get; set; }

        public string Edition { get; set; } = "XCOM2";

        public Dictionary<string, string> Shortnames { get; set; }

        public int SteamAppId { get; set; } = 268500;

        public Settings()
        {
            Editions = new Dictionary<string, XCom2Edition> {
                { "XCOM2", new XCom2Edition(
                    internalName: "XCOM2",
                    displayName: "<cyan>XCOM 2 (Base/Legacy)</cyan>",
                    steamAppName: "XCOM 2",
                    subFolderName: null,
                    sdkSteamAppName: "XCOM 2 SDK",
                    userGameFolderName: "XCOM2",
                    highlanderName: "X2CommunityHighlander") },
                { "WOTC", new XCom2Edition(
                    internalName: "WOTC",
                    displayName: "<red>War of the Chosen</red>",
                    steamAppName: "XCOM 2",
                    subFolderName: "XCom2-WarOfTheChosen",
                    sdkSteamAppName: "XCOM 2 War of the Chosen SDK",
                    userGameFolderName: "XCOM2 War of the Chosen",
                    highlanderName: "X2WOTCCommunityHighlander",
                    isExpansion: true) }
            };
            Shortnames = new Dictionary<string, string> {
                { "base", "XCOM2" },
                { "legacy", "XCOM2" },
                { "wotc", "WOTC" }
            };
        }

        public static void Load()
        {
            try
            {
                Default = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Path));
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is FileNotFoundException)
            {
                Default = new Settings();
            }
            catch (Exception ex)
            {
                Report.Exception(ex, "Settings could not be loaded; reverting to defaults");
                Default = new Settings();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                File.WriteAllText(Path, JsonConvert.SerializeObject(Default, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Report.Exception(ex, "Settings could not be saved");
            }
        }

        public class EditionDictJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => typeof(Dictionary<string, XCom2Edition>) == objectType;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
                var result = new Dictionary<string, XCom2Edition>();

                string editionInternalName = null;
                string editionDisplayName = null;
                string editionSteamAppName = null;
                string editionSubFolderName = null;
                string editionSdkSteamAppName = null;
                string editionUserGameFolderName = null;
                string editionHighlanderName = null;
                bool editionIsExpansion = false;
                string editionCustomSdkPath = null;
                string editionCustomGamePath = null;
                bool isInner = false;

                while (reader.Read()) {
                    if (reader.TokenType == JsonToken.StartObject) {
                        if (!isInner) {
                            isInner = true;
                        }
                    } else if (reader.TokenType == JsonToken.EndObject) {
                        if (isInner) {
                            isInner = false;
                            result.Add(editionInternalName, new XCom2Edition(
                                internalName: editionInternalName,
                                displayName: editionDisplayName,
                                steamAppName: editionSteamAppName,
                                subFolderName: editionSubFolderName,
                                sdkSteamAppName: editionSdkSteamAppName,
                                userGameFolderName: editionUserGameFolderName,
                                highlanderName: editionHighlanderName,
                                isExpansion: editionIsExpansion,
                                customSdkPath: editionCustomSdkPath,
                                customGamePath: editionCustomGamePath));
                            editionInternalName = null;
                            editionDisplayName = null;
                            editionSteamAppName = null;
                            editionSubFolderName = null;
                            editionSdkSteamAppName = null;
                            editionUserGameFolderName = null;
                            editionHighlanderName = null;
                            editionIsExpansion = false;
                            editionCustomSdkPath = null;
                            editionCustomGamePath = null;
                        } else {
                            break;
                        }
                    } else if (reader.TokenType == JsonToken.PropertyName) {
                        string currentPropertyName = (string)reader.Value;

                        if (isInner) {
                            reader.Read();

                            if (currentPropertyName == "IsExpansion") {
                                editionIsExpansion = (bool)reader.Value;
                            } else {
                                string currentPropertyValue = (string)reader.Value;
                                switch (currentPropertyName) {
                                    case "DisplayName":
                                        editionDisplayName = currentPropertyValue;
                                        break;
                                    case "SteamAppName":
                                        editionSteamAppName = currentPropertyValue;
                                        break;
                                    case "SubFolderName":
                                        editionSubFolderName = currentPropertyValue;
                                        break;
                                    case "SdkSteamAppName":
                                        editionSdkSteamAppName = currentPropertyValue;
                                        break;
                                    case "UserGameFolderName":
                                        editionUserGameFolderName = currentPropertyValue;
                                        break;
                                    case "HighlanderName":
                                        editionHighlanderName = currentPropertyValue;
                                        break;
                                    case "CustomSdkPath":
                                        editionCustomSdkPath = currentPropertyValue;
                                        break;
                                    case "CustomGamePath":
                                        editionCustomGamePath = currentPropertyValue;
                                        break;
                                }
                            }
                        } else {
                            editionInternalName = currentPropertyName;
                        }
                    }
                }

                return result;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
                writer.WriteStartObject();
                foreach (KeyValuePair<string, XCom2Edition> edition in Default.Editions) {
                    writer.WritePropertyName(edition.Key);
                    writer.WriteStartObject();

                    writer.WritePropertyName("DisplayName");
                    writer.WriteValue(edition.Value.DisplayName);

                    writer.WritePropertyName("SteamAppName");
                    writer.WriteValue(edition.Value.SteamAppName);

                    writer.WritePropertyName("SubFolderName");
                    writer.WriteValue(edition.Value.SubFolderName);

                    writer.WritePropertyName("SdkSteamAppName");
                    writer.WriteValue(edition.Value.SdkSteamAppName);

                    writer.WritePropertyName("UserGameFolderName");
                    writer.WriteValue(edition.Value.UserGameFolderName);

                    writer.WritePropertyName("HighlanderName");
                    writer.WriteValue(edition.Value.HighlanderName);

                    writer.WritePropertyName("IsExpansion");
                    writer.WriteValue(edition.Value.IsExpansion);

                    writer.WritePropertyName("CustomSdkPath");
                    writer.WriteValue(edition.Value.CustomSdkPath);

                    writer.WritePropertyName("CustomGamePath");
                    writer.WriteValue(edition.Value.CustomGamePath);

                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
        }
    }
}
