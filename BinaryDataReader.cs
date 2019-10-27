using System;
using System.IO;
using System.Linq;
using System.Text;

namespace XCom2ModTool
{
    internal abstract class BinaryDataReader : IDisposable
    {
        private bool leaveOpen;

        public BinaryDataReader(string path, bool leaveOpen = false)
        {
            Reader = new BinaryReader(File.OpenRead(path));
            this.leaveOpen = leaveOpen;
        }

        public BinaryDataReader(BinaryReader reader, bool leaveOpen = false)
        {
            Reader = reader;
            this.leaveOpen = leaveOpen;
        }

        public void Dispose()
        {
            if (!leaveOpen)
            {
                Reader?.Dispose();
            }
        }

        public void Detach()
        {
            if (!leaveOpen)
            {
                Reader?.Dispose();
            }
            Reader = null;
        }

        public void Attach(BinaryReader reader, bool leaveOpen = false)
        {
            Detach();
            Reader = reader;
            this.leaveOpen = leaveOpen;
        }

        public BinaryReader Reader { get; private set; }

        public bool EndOfStream => Position == Reader.BaseStream.Length;

        protected long Position
        {
            get => Reader.BaseStream.Position;
            set => Reader.BaseStream.Position = value;
        }

        protected byte[] Bytes(int count) => Reader.ReadBytes(count);
        protected byte[] Bytes(uint count)
        {
            if (count > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            return Reader.ReadBytes((int)count);
        }
        protected uint U32() => Reader.ReadUInt32();
        protected ushort U16() => Reader.ReadUInt16();
        protected byte U8() => Reader.ReadByte();
        protected bool Bool() => Reader.ReadUInt32() != 0;
        protected Guid FGuid() => new Guid(Bytes(16));

        protected string FString()
        {
            var length = Reader.ReadInt32();
            if (Math.Abs(length) > 4096)
            {
                //throw new InvalidDataException("Suspect FString exceeds 4096 bytes");
            }
            if (length < 0)
            {
                var bytes = Reader.ReadBytes(-length);
                return Encoding.Unicode.GetString(bytes, 0, bytes.Length - 2);
            }
            else
            {
                var bytes = Reader.ReadBytes(length);
                return Encoding.ASCII.GetString(bytes, 0, bytes.Length - 1);
            }
        }

        protected IDisposable Detour(long position = 0) => new BinaryDetour(Reader, position);

        protected T[] Array<T>(uint count, Func<T> construct)
        {
            if (count > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            return Enumerable.Range(0, (int)count).Select(x => construct()).ToArray();
        }

        protected T[] Array<T>(Func<T> construct) => Array(U32(), construct);

        private class BinaryDetour : IDisposable
        {
            private BinaryReader reader;
            private long position;

            public BinaryDetour(BinaryReader reader, long newPosition)
            {
                this.reader = reader;
                this.position = reader.BaseStream.Position;
                reader.BaseStream.Position = newPosition;
            }

            public void Dispose()
            {
                reader.BaseStream.Position = position;
            }
        }
    }
}
