using System;
using Newtonsoft.Json;

namespace XCom2ModTool
{
    internal class HexJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(uint).Equals(objectType);

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
