using System;

namespace XCom2ModTool.UnrealPackages
{
    [Flags]
    internal enum PackageFlags : uint
    {
        None            = 0u,

        AllowDownload   = 0x1u,
        ClientOptional  = 0x2u,
        ServerSideOnly  = 0x4u,
        Cooked          = 0x8u, // or BrokenLinks

        Insecure        = 0x10u,
        Encrypted       = 0x20u,
        Unknown40       = 0x40u,
        Unknown80       = 0x80u,

        Unknown100      = 0x100u,
        Unknown200      = 0x200u,
        Unknown400      = 0x400u,
        Unknown800      = 0x800u,

        Unknown1K       = 0x1000u,
        Unknown2K       = 0x2000u,
        Unknown4K       = 0x4000u,
        Required        = 0x8000u,

        Unknown10K      = 0x10000u,
        Map             = 0x20000u,
        Unknown40K      = 0x40000u,
        Unknown80K      = 0x80000u,

        Unknown100K     = 0x100000u,
        Script          = 0x200000u,
        Debug           = 0x400000u,
        Imports         = 0x800000u,

        Unknown1M       = 0x1000000u,
        Compressed      = 0x2000000u,
        FullyCompressed = 0x4000000u,
        Unknown8M       = 0x8000000u,

        Unknown10M      = 0x10000000u,
        NoExportsData   = 0x20000000u,
        Stripped        = 0x40000000u,
        Protected       = 0x80000000u,
    };
}
