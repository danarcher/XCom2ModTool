using Newtonsoft.Json;

namespace XCom2ModTool.UnrealPackages
{
    [JsonConverter(typeof(ObjectReferenceJsonConverter))]
    internal class ObjectReference
    {
        public int Raw { get; set; }
        public PackageReferenceable To { get; set; }
    }
}
