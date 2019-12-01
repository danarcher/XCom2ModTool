using System;
using Newtonsoft.Json;

namespace XCom2ModTool
{
    internal class StringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override bool CanRead => false;
        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}