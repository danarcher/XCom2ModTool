using Newtonsoft.Json;

namespace XCom2ModTool.UnrealPackages
{
    internal class PackageHeader
    {
        public bool IsDebug => PackageFlags.HasFlag(PackageFlags.Debug);

        [JsonConverter(typeof(HexJsonConverter))]
        public uint Signature { get; set; }
        public ushort Version { get; set; }
        public ushort LicenseeVersion { get; set; }
        public uint HeaderSize { get; set; }
        public string PackageGroup { get; set; }
        public PackageFlags PackageFlags { get; set; }
        //public uint NameCount;
        //public uint NameTableOffset;
        //public uint ExportCount;
        //public uint ExportTableOffset;
        //public uint ImportCount;
        //public uint ImportTableOffset;
        //public uint DependsOffset;
        //public uint SerializedOffset; // equal to HeaderSize
        //public uint Unknown2;
        //public uint Unknown3;
        //public uint Unknown4;
        //public Guid PackageGuid;
        //public GenerationInfo[] Generations;
        //public uint EngineVersion;
        //public uint CookerVersion;
        //public uint CompressionFlags;
        //public CompressedChunk[] CompressedChunks;
        //// end of header

        public override string ToString()
        {
            return $"Version={Version}/{LicenseeVersion}, Group={PackageGroup}, Flags={PackageFlags.ToString().Replace(", ", "|")}";
        }

        //public NameTableEntry[] NameTable;
        //public ImportTableEntry[] ImportTable;
        //public ExportTableEntry[] ExportTable;

        //public class GenerationInfo
        //{
        //    public uint ExportCount;
        //    public uint NameCount;
        //    public uint NetObjectCount;
        //}

        //public class NameTableEntry
        //{
        //    public string Name;
        //    public ulong Flags;
        //}

        //public enum ObjectReference : uint { }

        //public class ImportTableEntry
        //{
        //    public NameIndex PackageIndex;
        //    public NameIndex TypeIndex;
        //    public ObjectReference OwnerReference;
        //    public NameIndex NameIndex;
        //}

        //public class ExportTableEntry
        //{
        //    public ObjectReference TypeReference;
        //    public ObjectReference ParentClassReference;
        //    public ObjectReference OwnerReference;
        //    public NameIndex NameIndex;
        //    public ObjectReference ArchetypeReference;
        //    public uint ObjectFlagsHigh;
        //    public uint ObjectFlagsLow;
        //    public uint SerializedDataSize;
        //    public uint SerializedDataOffset;
        //    public uint ExportFlags;
        //    public uint NetObjectCount;
        //    public Guid NetGuid; // zero unless NetObjectCount > 0
        //    public uint NetUnknown; // zero unless NetObjectCount > 0
        //    public uint[] NetObjectUnknown; // 4 x NetObjectCount bytes of data
        //}

        //public class DependsTableEntry
        //{
        //    public byte[] Unknown; // (HeaderSize - DependsOffset) bytes of unknown data, which may be zero for XCOM.
        //}

        //public class UnrealObject
        //{
        //    public uint NetIndex;
        //    public byte[] StackUnknown; // 22 bytes of unknown data, only if HasStack flag is set
        //    public UnrealDefaultPropertyList DefaultProperties; // non-Class objects only
        //}

        //public class UnrealField : UnrealObject
        //{
        //    public ObjectReference NextFieldReference;
        //    public ObjectReference ParentReference; // Struct objects only
        //}

        //public class UnrealConstant : UnrealField
        //{
        //}

        //public class UnrealEnum : UnrealField
        //{
        //}

        //public class UnrealProperty : UnrealField
        //{
        //}

        //public class UnrealByteProperty : UnrealProperty
        //{
        //}

        //public class UnrealIntProperty : UnrealProperty
        //{
        //}

        //public class UnrealBoolProperty : UnrealProperty
        //{
        //}

        //public class UnrealFloatProperty : UnrealProperty
        //{
        //}

        //public class UnrealObjectProperty : UnrealProperty
        //{
        //}

        //public class UnrealClassProperty : UnrealObjectProperty
        //{
        //}

        //public class UnrealComponentProperty : UnrealObjectProperty
        //{
        //}

        //public class UnrealNameProperty : UnrealProperty
        //{
        //}

        //public class UnrealStructProperty : UnrealProperty
        //{
        //}

        //public class UnrealStringProperty : UnrealProperty
        //{
        //}

        //public class UnrealFixedArrayProperty : UnrealProperty
        //{
        //}

        //public class UnrealArrayProperty : UnrealProperty
        //{
        //}

        //public class UnrealMapProperty : UnrealProperty
        //{
        //}

        //public class UnrealDelegateProperty : UnrealProperty
        //{
        //}

        //public class UnrealInterfaceProperty : UnrealProperty
        //{
        //}

        //public class UnrealStruct : UnrealField
        //{
        //    public ObjectReference ScriptTextReference;
        //    public ObjectReference FirstChildReference;
        //    public ObjectReference CppTextReference;
        //    public uint Line;
        //    public uint TextPosition;
        //    public uint ScriptMemorySize;
        //    public uint ScriptSerializedSize;
        //    public byte[] ScriptData; // ScriptSerializedSize bytes
        //}

        //public class UnrealDefaultPropertyList
        //{
        //}

        //public class UnrealScriptStruct : UnrealStruct
        //{
        //    public uint StructFlags;
        //    public UnrealDefaultPropertyList StructDefaultProperties; // TODO: dupe?
        //}

        //public class UnrealFunction : UnrealStruct
        //{
        //    public ushort NativeToken;
        //    public byte OperatorPrecedence;
        //    public uint FunctionFlags;
        //    public ushort RepOffset; // Only if FunctionFlags.Net is set, else omitted
        //    public NameIndex NameIndex;
        //}

        //public class UnrealState : UnrealStruct
        //{
        //    public uint ProbeMask;
        //    public ushort LabelTableOffset; // offset of LabelTable inside DataScript
        //    public uint StateFlags; // TODO: ushort?



        //}

        //public class UnrealClass : UnrealState
        //{
        //}

        //public class UnrealTextBuffer : UnrealObject
        //{
        //}

        //public class UnrealComponent : UnrealObject
        //{
        //}

        //public struct NameIndex
        //{
        //    public uint Index;
        //    public uint NumericSuffixPlusOne; // 0 or none; 1 for "_0", 2 for "_1", etc.
        //}
    }
}
