using System;
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

            //NameCount = U32();
            //NameTableOffset = U32();
            //ExportCount = U32();
            //ExportTableOffset = U32();
            //ImportCount = U32();
            //ImportTableOffset = U32();
            //DependsOffset = U32();
            //SerializedOffset = U32();
            //Unknown2 = U32();
            //Unknown3 = U32();
            //Unknown4 = U32();
            //PackageGuid = FGuid();
            //Generations = Array(() => new GenerationInfo
            //{
            //    ExportCount = U32(),
            //    NameCount = U32(),
            //    NetObjectCount = U32()
            //});
            //EngineVersion = U32();
            //CookerVersion = U32();
            //CompressionFlags = U32();
            //CompressedChunks = Array(() => new CompressedChunk
            //{
            //    UncompressedOffset = U32(),
            //    UncompressedSize = U32(),
            //    CompressedOffset = U32(),
            //    CompressedSize = U32()
            //});

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
