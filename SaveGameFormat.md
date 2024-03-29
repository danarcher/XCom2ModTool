﻿# XCOM 2 Save Game File Format

Documented by Dan Archer, https://github.com/danarcher. This information was
initially pieced together through elementary reverse engineering, with later
help from online sources regarding the XCOM: Enemy Unknown save format,
http://wiki.tesnexus.com/index.php/Savegame_file_format_-_XCOM:EU_2012.

XCOM 2 save games consist of a variable-length header followed immediately by a
series of compressed chunks of data.

On the PC at least, the format is little-endian.

Later versions of the game (e.g. WoTC) use a higher version number and add
additional fields to the end of the header.

The structure is similar, though not identical, to the XCOM 2 SDK native struct
SaveGameHeader. See your XCOM 2 SDK's
Development\Src\Engine\Classes\OnlineSubsystem.uc. Though unreliable as to early
order and contents, it does seem accurate in so far as identifying new fields
(if not their order) added in later versions of the save game format after
DLCPackFriendlyNames.

## Data Types

| Type      | Length | Description |
| --------- | ------ | ----------- |
| DWORD     | 4      | An unsigned 32-bit integer |
| BOOL      | 4      | 0 = false, 1 = true |
| CZStr     | 1+     | A null-terminated ANSI string |
| FString   | 5+     | A DWORD string buffer length (including the terminating null) followed by a CZStr |
| *type*[]  | 0+     | An array of *type*s with no length indicator |
| *type*[n] | 4+     | A DWORD array length followed by that number of *type*s |

If an FString's length is negative, then its true length is positive and its
characters are UTF-16 rather than ANSI.

## Save Game Header

| Version | Offset | Type       | Description |
| ------- | ------ | ---------  | ----------- |
| 20+     | 0      | DWORD      | Version (20 for base game, 21 for WoTC, 22 for Recent WoTC / TLP) |
| 20+     | 4      | DWORD      | Header byte count, i.e. offset to compressed chunks from the start of the file |
| 20+     | 8      | DWORD      | Header checksum (uses the BZip2 CRC32 algorithm) |
| 20+     | 12     | DWORD      | Uncompressed Size (may be zero) |
| 20+     | 16     | DWORD      | Campaign Number |
| 20+     | 20     | DWORD      | Save Slot Number |
| 20+     | 28     | FString    | Description (Date\nTime\nPlayer Save Name\nMission Type Name\nOperation Name\nGame Date\nGame Time\sMap Name\s) |
| 20+     | -      | FString    | Save DateTime (Date\nTime) |
| 20+     | -      | FString    | Map Command |
| 20+     | -      | BOOL       | Tactical (0=Strategy, 1=Tactical) |
| 20+     | -      | BOOL       | Ironman (0=Normal, 1=Ironman)|
| 20+     | -      | BOOL       | Autosave (0=Manual, 1=Autosave) |
| 20+     | -      | BOOL       | Quicksave (0=Normal, 1=Quick)|
| 20+     | -      | FString    | Language (e.g. "INT") |
| 20+     | -      | DWORD      | Unknown6 (Negative number? Random seed?) |
| 20+     | -      | DWORD      | Unknown7 (0) |
| 20+     | -      | DWORD      | ArchiveFileVersion (845) |
| 20+     | -      | DWORD      | ArchiveLicenseeVersion (e.g. 108 for base game, 120 for WoTC, ...?) |
| 20+     | -      | FString    | Campaign Start DateTime (e.g. 2019.08.05-21.16.32.2) |
| 20+     | -      | FString    | Mission Image URI |
| 20+     | -      | FString    | Player Save Name (again) |
| 20+     | -      | FString[n] | DLC Pack Names |
| 20+     | -      | FString[n] | DLC Pack Friendly Names |
| 21+     | -      | DWORD      | Mission Number (-1 for Tactical Quick Launch) |
| 21+     | -      | DWORD      | Month |
| 21+     | -      | DWORD      | Turn |
| 21+     | -      | DWORD      | Action |
| 21+     | -      | FString    | Mission Type |
| 21+     | -      | BOOL       | Debug Save (0=Normal, 1=Debug) |
| 21+     | -      | BOOL       | Pre Mission |
| 21+     | -      | BOOL       | Post Mission |
| 22+     | -      | BOOL       | Ladder |

## Compressed Chunks

An unspecified number of compressed chunks of data immediately follow the header
(if a particular header version has been read correctly and completely.)

One can keep reading successive compressed chunks from the file until the end
of the file. There should be no excess data in the save file.

Each compressed chunk begins with the standard UPK four byte signature 0xC1,
0x83, 0x2A, 0x9E, aka the little-endian DWORD value 0x9E2A83C1.

| Offset | Type              | Description             |
| ------ | ----------------- | ----------------------- |
| 0      | DWORD             | Signature (0x9E2A83C1)  |
| 4      | DWORD             | Block Size              |
| 8      | DWORD             | Total Compressed Size   |
| 12     | DWORD             | Total Uncompressed Size |
| 16     | CompressedBlock[] | Compressed Blocks       |

The number of compressed blocks is equal to the total uncompressed size divided
by the block size, rounding up.

There tends to be only one compressed block per chunk, though the format permits
more.

## Compressed Blocks

| Offset | Type    | Description       |
| ------ | ------- | ----------------- |
| 0      | DWORD   | Compressed Size   |
| 4      | DWORD   | Uncompressed Size |
| 8      | BYTE[]  | Compressed Data   |

Blocks are compressed with a platform-dependent compression algorithm. On PC
(and possibly on consoles, though this is untested), XCOM 2 uses LZO1X_1
compression. iOS versions of XCOM: Enemy Unknown used zlib compression.

## Compression Algorithm

LZO1X_1 is an open source but largely undocumented compression format, read only
by its own implementation. The current library version at time of writing, 2.10,
is available from its author's site, http://www.oberhumer.com/opensource/lzo/.
This is distributed as portable source code and can be built for Linux or
Windows using common compilers. There is no precompiled binary distribution from
the author. This library is GPL.

C# ports and wrappers around this library are scarce, unmaintained, and those
tested proved unreliable on XCOM 2 save data. It is recommended to wrap calls to
the native LZO.DLL, once built from source, using interop and PInvoke.

To compress or decompress data, the only required calls are to
\_\_lzo\_init\_v2(1, -1, -1, -1, -1, -1, -1, -1, -1, -1) (once) and then either
lzo1x\_decompress(src, src_len, dst, dst_len, wrkmem), or lzo1x\_compress()
which takes the same arguments. Source and destination buffers are byte arrays,
and lengths are the known compressed and uncompressed sizes to be/stored in each
compressed block. The compressor requires a 64k wrkmem buffer on 32 bit
operating systems (128k on 64-bit operating systems), and returns zero on
success and non-zero on failure. The decompressor does not require a wrkmem
buffer (null is acceptable) and returns the same values.

Return values and 32-bit PInvoke signatures are below. For a 64-bit build,
remove the CallingConvention parameters from the DllImport attributes.

    const int LZO_E_OK = 0;
    const int LZO_E_ERROR = -1;
    const int LZO_E_OUT_OF_MEMORY = -2;
    const int LZO_E_NOT_COMPRESSIBLE = -3;
    const int LZO_E_INPUT_OVERRUN = -4;
    const int LZO_E_OUTPUT_OVERRUN = -5;
    const int LZO_E_LOOKBEHIND_OVERRUN = -6;
    const int LZO_E_EOF_NOT_FOUND = -7;
    const int LZO_E_INPUT_NOT_CONSUMED = -8;

    [DllImport("lzo2.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern int __lzo_init_v2(uint v, int s1, int s2, int s3, int s4, int s5, int s6, int s7, int s8, int s9);

    [DllImport("lzo2.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern int lzo1x_1_compress(byte[] src, int src_len, byte[] dst, ref int dst_len, byte[] wrkmem);

    [DllImport("lzo2.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern int lzo1x_decompress(byte[] src, int src_len, byte[] dst, ref int dst_len, byte[] wrkmem);

## CRC Calculation

This is the standard algorithm used by BZip2 to compute its CRCs. It is not
the same as the vanilla CRC32 algorithm. Some online sources refer to this as
"CRC32B", though I'm not convinced it's the only algorithm with that moniker.

C# source follows.

    private static readonly uint[] Table = {
        0x00000000U, 0x04c11db7U, 0x09823b6eU, 0x0d4326d9U,
        0x130476dcU, 0x17c56b6bU, 0x1a864db2U, 0x1e475005U,
        0x2608edb8U, 0x22c9f00fU, 0x2f8ad6d6U, 0x2b4bcb61U,
        0x350c9b64U, 0x31cd86d3U, 0x3c8ea00aU, 0x384fbdbdU,
        0x4c11db70U, 0x48d0c6c7U, 0x4593e01eU, 0x4152fda9U,
        0x5f15adacU, 0x5bd4b01bU, 0x569796c2U, 0x52568b75U,
        0x6a1936c8U, 0x6ed82b7fU, 0x639b0da6U, 0x675a1011U,
        0x791d4014U, 0x7ddc5da3U, 0x709f7b7aU, 0x745e66cdU,
        0x9823b6e0U, 0x9ce2ab57U, 0x91a18d8eU, 0x95609039U,
        0x8b27c03cU, 0x8fe6dd8bU, 0x82a5fb52U, 0x8664e6e5U,
        0xbe2b5b58U, 0xbaea46efU, 0xb7a96036U, 0xb3687d81U,
        0xad2f2d84U, 0xa9ee3033U, 0xa4ad16eaU, 0xa06c0b5dU,
        0xd4326d90U, 0xd0f37027U, 0xddb056feU, 0xd9714b49U,
        0xc7361b4cU, 0xc3f706fbU, 0xceb42022U, 0xca753d95U,
        0xf23a8028U, 0xf6fb9d9fU, 0xfbb8bb46U, 0xff79a6f1U,
        0xe13ef6f4U, 0xe5ffeb43U, 0xe8bccd9aU, 0xec7dd02dU,
        0x34867077U, 0x30476dc0U, 0x3d044b19U, 0x39c556aeU,
        0x278206abU, 0x23431b1cU, 0x2e003dc5U, 0x2ac12072U,
        0x128e9dcfU, 0x164f8078U, 0x1b0ca6a1U, 0x1fcdbb16U,
        0x018aeb13U, 0x054bf6a4U, 0x0808d07dU, 0x0cc9cdcaU,
        0x7897ab07U, 0x7c56b6b0U, 0x71159069U, 0x75d48ddeU,
        0x6b93dddbU, 0x6f52c06cU, 0x6211e6b5U, 0x66d0fb02U,
        0x5e9f46bfU, 0x5a5e5b08U, 0x571d7dd1U, 0x53dc6066U,
        0x4d9b3063U, 0x495a2dd4U, 0x44190b0dU, 0x40d816baU,
        0xaca5c697U, 0xa864db20U, 0xa527fdf9U, 0xa1e6e04eU,
        0xbfa1b04bU, 0xbb60adfcU, 0xb6238b25U, 0xb2e29692U,
        0x8aad2b2fU, 0x8e6c3698U, 0x832f1041U, 0x87ee0df6U,
        0x99a95df3U, 0x9d684044U, 0x902b669dU, 0x94ea7b2aU,
        0xe0b41de7U, 0xe4750050U, 0xe9362689U, 0xedf73b3eU,
        0xf3b06b3bU, 0xf771768cU, 0xfa325055U, 0xfef34de2U,
        0xc6bcf05fU, 0xc27dede8U, 0xcf3ecb31U, 0xcbffd686U,
        0xd5b88683U, 0xd1799b34U, 0xdc3abdedU, 0xd8fba05aU,
        0x690ce0eeU, 0x6dcdfd59U, 0x608edb80U, 0x644fc637U,
        0x7a089632U, 0x7ec98b85U, 0x738aad5cU, 0x774bb0ebU,
        0x4f040d56U, 0x4bc510e1U, 0x46863638U, 0x42472b8fU,
        0x5c007b8aU, 0x58c1663dU, 0x558240e4U, 0x51435d53U,
        0x251d3b9eU, 0x21dc2629U, 0x2c9f00f0U, 0x285e1d47U,
        0x36194d42U, 0x32d850f5U, 0x3f9b762cU, 0x3b5a6b9bU,
        0x0315d626U, 0x07d4cb91U, 0x0a97ed48U, 0x0e56f0ffU,
        0x1011a0faU, 0x14d0bd4dU, 0x19939b94U, 0x1d528623U,
        0xf12f560eU, 0xf5ee4bb9U, 0xf8ad6d60U, 0xfc6c70d7U,
        0xe22b20d2U, 0xe6ea3d65U, 0xeba91bbcU, 0xef68060bU,
        0xd727bbb6U, 0xd3e6a601U, 0xdea580d8U, 0xda649d6fU,
        0xc423cd6aU, 0xc0e2d0ddU, 0xcda1f604U, 0xc960ebb3U,
        0xbd3e8d7eU, 0xb9ff90c9U, 0xb4bcb610U, 0xb07daba7U,
        0xae3afba2U, 0xaafbe615U, 0xa7b8c0ccU, 0xa379dd7bU,
        0x9b3660c6U, 0x9ff77d71U, 0x92b45ba8U, 0x9675461fU,
        0x8832161aU, 0x8cf30badU, 0x81b02d74U, 0x857130c3U,
        0x5d8a9099U, 0x594b8d2eU, 0x5408abf7U, 0x50c9b640U,
        0x4e8ee645U, 0x4a4ffbf2U, 0x470cdd2bU, 0x43cdc09cU,
        0x7b827d21U, 0x7f436096U, 0x7200464fU, 0x76c15bf8U,
        0x68860bfdU, 0x6c47164aU, 0x61043093U, 0x65c52d24U,
        0x119b4be9U, 0x155a565eU, 0x18197087U, 0x1cd86d30U,
        0x029f3d35U, 0x065e2082U, 0x0b1d065bU, 0x0fdc1becU,
        0x3793a651U, 0x3352bbe6U, 0x3e119d3fU, 0x3ad08088U,
        0x2497d08dU, 0x2056cd3aU, 0x2d15ebe3U, 0x29d4f654U,
        0xc5a92679U, 0xc1683bceU, 0xcc2b1d17U, 0xc8ea00a0U,
        0xd6ad50a5U, 0xd26c4d12U, 0xdf2f6bcbU, 0xdbee767cU,
        0xe3a1cbc1U, 0xe760d676U, 0xea23f0afU, 0xeee2ed18U,
        0xf0a5bd1dU, 0xf464a0aaU, 0xf9278673U, 0xfde69bc4U,
        0x89b8fd09U, 0x8d79e0beU, 0x803ac667U, 0x84fbdbd0U,
        0x9abc8bd5U, 0x9e7d9662U, 0x933eb0bbU, 0x97ffad0cU,
        0xafb010b1U, 0xab710d06U, 0xa6322bdfU, 0xa2f33668U,
        0xbcb4666dU, 0xb8757bdaU, 0xb5365d03U, 0xb1f740b4U
    };

    public static uint ComputeCRC(byte[] data)
    {
        var crc = uint.MaxValue;
        foreach (var b in data)
        {
            var index = ((crc >> 24) ^ b) & 0xff;
            crc = Table[index] ^ (crc << 8);
        }
        return ~crc;
    }
