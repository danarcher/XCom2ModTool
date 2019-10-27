using System;

namespace XCom2ModTool.UnrealPackages
{
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
