using System;
using Newtonsoft.Json;

namespace XCom2ModTool.UnrealPackages
{
    [JsonConverter(typeof(StringJsonConverter))]
    internal class PackageExport : PackageReferenceable
    {
        public ObjectReference Type { get; set; }
        public ObjectReference ParentClass { get; set; }
        public ObjectReference Owner { get; set; }
        public string Name { get; set; }
        public ObjectReference Archetype { get; set; }
        public ulong ObjectFlags { get; set; }
        public uint SerializedDataSize { get; set; }
        public uint SerializedDataOffset { get; set; }
        public uint ExportFlags { get; set; }
        public uint NetObjectCount { get; set; }
        public Guid Guid { get; set; }
        public uint Unknown { get; set; }
        public uint[] NetUnknown { get; set; }

        public override string ToString()
        {
            if (Owner?.To != null)
            {
                return Owner.To.ToString() + "." + Name;
            }
            return Name;
        }
    }
}
