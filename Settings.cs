using System;
using System.IO;
using System.Linq;
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

        [JsonConverter(typeof(EditionJsonConverter))]
        public XCom2Edition Edition { get; set; } = XCom2.Base;

        public bool UseColoredOutput { get; set; } = true;

        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;

        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor WarningColor { get; set; } = ConsoleColor.Yellow;

        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor InfoColor { get; set; } = ConsoleColor.Gray;

        [JsonConverter(typeof(StringEnumConverter))]
        public ConsoleColor VerboseColor { get; set; } = ConsoleColor.Gray;

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

        public class EditionJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => typeof(XCom2Edition) == objectType;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var internalName = (string)reader.Value;
                return XCom2.Editions.First(x => x.InternalName == internalName);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var edition = (XCom2Edition)value;
                writer.WriteValue(edition.InternalName);
            }
        }
    }
}
