using System;
using System.Text;
using Newtonsoft.Json;

namespace XCom2ModTool
{
    internal class AbbreviatedByteArrayJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(byte[]).Equals(objectType);

        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException($"{nameof(AbbreviatedByteArrayJsonConverter)} cannot read JSON data");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var builder = new StringBuilder();
            var bytes = (byte[])value;
            var dotCount = 0;
            foreach (var b in bytes)
            {
                if (b >= 32 && b <= 126)
                {
                    dotCount = 0;
                    builder.Append((char)b);
                }
                else if (dotCount < 2)
                {
                    builder.Append(".");
                    ++dotCount;
                }
            }
            writer.WriteValue(builder.ToString());
        }
    }
}
