using System;
using System.IO;

namespace XCom2ModTool
{
    internal class PackageInfo
    {
        private const uint ValidSignature = 0x9E2A83C1;

        public PackageInfo(string path)
        {
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                try
                {
                    Signature = reader.ReadUInt32();
                    if (Signature == ValidSignature)
                    {
                        Version = reader.ReadUInt16();
                        LicenseeVersion = reader.ReadUInt16();
                        HeaderSize = reader.ReadUInt32();
                        Group = ReadFString(reader);
                        Flags = (PackageFlags)reader.ReadUInt32();
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private static string ReadFString(BinaryReader reader)
        {
            var text = string.Empty;
            var length = reader.ReadInt32();
            if (length < 0)
            {
                for (var i = 0; i <= -length; ++i)
                {
                    var c = (char)reader.ReadUInt16();
                    if (c == 0) break;
                    text += c;
                }
            }
            else
            {
                for (var i = 0; i <= length; ++i)
                {
                    var c = (char)reader.ReadByte();
                    if (c == 0) break;
                    text += c;
                }
            }
            return text;
        }

        public override string ToString()
        {
            if (!IsValid)
            {
                return "(Invalid Package)";
            }
            else
            {
                return $"Version={Version}/{LicenseeVersion}, Group={Group}, Flags={Flags.ToString().Replace(", ", "|")}";
            }
        }

        public bool IsValid => Signature == ValidSignature;
        public bool IsDebug => Flags.HasFlag(PackageFlags.Debug);

        public uint Signature;
        public ushort Version;
        public ushort LicenseeVersion;
        public uint HeaderSize;
        public string Group;
        public PackageFlags Flags;
    }

    [Flags]
    internal enum PackageFlags : uint
    {
        None = 0u,
        AllowDownload = 0x1u,
        ClientOptional = 0x2u,
        ServerSideOnly = 0x4u,
        Cooked = 0x8u, // or BrokenLinks
        Insecure = 0x10u,
        Encrypted = 0x20u,
        Required = 0x8000u,
        Map = 0x20000u,
        Script = 0x200000u,
        Debug = 0x400000u,
        Imports = 0x800000u,
        Compressed = 0x02000000u,
        FullyCompressed = 0x04000000u,
        NoExportsData = 0x20000000u,
        Stripped = 0x40000000u,
        Protected = 0x80000000u,
    };
}
