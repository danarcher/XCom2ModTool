using System;
using System.Collections.Generic;
using System.IO;

namespace XCom2ModTool.UnrealPackages
{
    internal class PackageReader : BinaryDataReader
    {
        public PackageReader(string path, bool leaveOpen = false) : base(path, leaveOpen) { }

        public PackageReader(BinaryReader reader, bool leaveOpen = false) : base(reader, leaveOpen) { }

        public PackageHeader ReadHeader()
        {
            var header = new PackageHeader();
            header.Signature = U32();
            if (header.Signature != PackageSignature.Valid)
            {
                throw new InvalidDataException("Invalid package signature");
            }

            header.Version = U16();
            header.LicenseeVersion = U16();
            header.HeaderSize = U32();
            header.PackageGroup = FString();
            header.PackageFlags = (PackageFlags)U32();

            var nameCount = U32();
            var nameOffset = U32();
            using (Detour(nameOffset))
            {
                var names = new List<GlobalName>();
                for (var i = 0; i < nameCount; ++i)
                {
                    var name = FString();
                    var flags = U64();
                    names.Add(new GlobalName { Name = name, Flags = flags });
                }
                header.Names = names.ToArray();
            }

            var exportCount = U32();
            var exportOffset = U32();
            using (Detour(exportOffset))
            {
                header.Exports = Array(exportCount, () =>
                {
                    return new PackageExport
                    {
                        Type = Ref(),
                        ParentClass = Ref(),
                        Owner = Ref(),
                        Name = Name(header.Names),
                        Archetype = Ref(),
                        ObjectFlags = U64HL(),
                        SerializedDataSize = U32(),
                        SerializedDataOffset = U32(),
                        ExportFlags = U32(),
                        NetObjectCount = Push(U32()),
                        Guid = FGuid(),
                        Unknown = U32(),
                        NetUnknown = Array(Pop(), () => U32())
                    };
                });
            }

            var importCount = U32();
            var importOffset = U32();
            using (Detour(importOffset))
            {
                header.Imports = Array(importCount, () =>
                {
                    return new PackageImport
                    {
                        PackageName = Name(header.Names),
                        TypeName = Name(header.Names),
                        Owner = Ref(),
                        Name = Name(header.Names),
                    };
                });
            }

            foreach (var objRef in Refs())
            {
                if (objRef.Raw > 0)
                {
                    objRef.To = header.Exports[objRef.Raw - 1];
                }
                else if (objRef.Raw < 0)
                {
                    objRef.To = header.Imports[(-objRef.Raw) - 1];
                }
            }

            return header;
        }

        public CompressedChunk ReadCompressedChunk()
        {
            var chunk = new CompressedChunk();
            chunk.Signature = U32();
            if (chunk.Signature != PackageSignature.Valid)
            {
                throw new InvalidDataException("Invalid compressed chunk signature");
            }
            chunk.BlockSize = U32();
            chunk.CompressedSize = U32();
            chunk.UncompressedSize = U32();
            var blockCount = (uint)Math.Ceiling(chunk.UncompressedSize / (double)chunk.BlockSize);
            chunk.Blocks = Array(blockCount, () =>
            {
                var block = new CompressedBlock
                {
                    CompressedSize = U32(),
                    UncompressedSize = U32()
                };
                var data = Bytes(block.CompressedSize);
                var decompressed = new byte[block.UncompressedSize];
                var output = new byte[block.UncompressedSize];
                block.Data = Lzo.Decompress(data, output);
                return block;
            });

            return chunk;
        }
    }
}
