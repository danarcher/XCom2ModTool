using System;
using Newtonsoft.Json;

namespace XCom2ModTool.UnrealPackages
{
    internal class ObjectReferenceJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(ObjectReference) == objectType;

        public override bool CanRead => false;
        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var objRef = (ObjectReference)value;
            if (objRef?.To != null)
            {
                serializer.Serialize(writer, objRef.To);
            }
            else
            {
                writer.WriteValue(string.Empty);
            }
        }
    }
}