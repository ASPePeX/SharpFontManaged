using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpFontManaged
{
    internal sealed unsafe class DataReader : IDisposable
    {
        private readonly Stream stream;
        private readonly byte[] buffer;
        private readonly GCHandle handle;
        private readonly byte* start;
        private readonly int maxReadLength;
        private int readOffset;
        private int writeOffset;

        public uint Position => (uint)(stream.Position - (writeOffset - readOffset));

        public DataReader(Stream stream, int maxReadLength = 4096)
        {
            this.stream = stream;
            this.maxReadLength = maxReadLength;

            buffer = new byte[maxReadLength * 2];
            handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            start = (byte*)handle.AddrOfPinnedObject();
        }       

        public byte ReadByte()
        {
            return *Read(1);
        }

        public sbyte ReadSByte()
        {
            return *(sbyte*)Read(1);
        }

        public short ReadInt16()
        {
            return *(short*)Read(sizeof(short));
        }

        public int ReadInt32()
        {
            return *(int*)Read(sizeof(int));
        }

        public ushort ReadUInt16()
        {
            return *(ushort*)Read(sizeof(ushort));
        }

        public uint ReadUInt32()
        {
            return *(uint*)Read(sizeof(uint));
        }

        public short ReadInt16BE()
        {
            return (short)Htons(ReadUInt16());
        }

        public int ReadInt32BE()
        {
            return (int)Htonl(ReadUInt32());
        }

        public ushort ReadUInt16BE()
        {
            return Htons(ReadUInt16());
        }

        public uint ReadUInt32BE()
        {
            return Htonl(ReadUInt32());
        }

        public byte[] ReadBytes(int count)
        {
            var result = new byte[count];
            var index = 0;
            while (count > 0)
            {
                var readCount = Math.Min(count, maxReadLength);
                Marshal.Copy(new IntPtr(Read(readCount)), result, index, readCount);

                count -= readCount;
                index += readCount;
            }
            return result;
        }

        public void Seek(uint position)
        {
            // if the position is within our buffer we can reuse part of it
            // otherwise, just clear everything out and jump to the right spot
            var current = stream.Position;
            if (position < current - writeOffset || position >= current)
            {
                readOffset = 0;
                writeOffset = 0;
                stream.Position = position;
            }
            else
            {
                readOffset = (int)(position - current + writeOffset);
                CheckWrapAround(0);
            }
        }

        public void Skip(int count)
        {
            readOffset += count;
            if (readOffset < writeOffset)
            {
                CheckWrapAround(0);
            }
            else
            {
                // we've skipped everything in our buffer; clear it out
                // and then skip any remaining data by seeking the stream
                var seekCount = readOffset - writeOffset;
                if (seekCount > 0)
                    stream.Position += seekCount;

                readOffset = 0;
                writeOffset = 0;
            }
        }

        private byte* Read(int count)
        {
            // we'll be returning a pointer to a contiguous block of memory
            // at least count bytes large, starting at the current offset
            var result = start + readOffset;
            readOffset += count;

            if (readOffset >= writeOffset)
            {
                if (count > maxReadLength)
                    throw new InvalidOperationException("Tried to read more data than the max read length.");

                // we need to read at least this many bytes, but we'll try for more (could be zero)
                var need = readOffset - writeOffset;
                while (need > 0)
                {
                    // try to read in a chunk of maxReadLength bytes (unless that would push past the end of our space)
                    var read = stream.Read(buffer, writeOffset, Math.Min(maxReadLength, buffer.Length - writeOffset));
                    if (read <= 0)
                        throw new EndOfStreamException();

                    writeOffset += read;
                    need -= read;
                }

                if (CheckWrapAround(count))
                    result = start;
            }

            // most of the time we'll have plenty of data in the buffer
            // so we'll fall through here and get the pointer quickly
            return result;
        }

        private bool CheckWrapAround(int dataCount)
        {
            // if we've gone past the max read length, we can no longer ensure
            // that future read calls of maxReadLength size will be able to get a
            // contiguous buffer, so wrap back to the beginning
            if (readOffset >= maxReadLength)
            {
                // back copy any buffered data so that it doesn't get lost
                var copyCount = writeOffset - readOffset + dataCount;
                if (copyCount > 0)
                    Buffer.BlockCopy(buffer, readOffset - dataCount, buffer, 0, copyCount);

                readOffset = dataCount;
                writeOffset = copyCount;
                return true;
            }

            return false;
        }

        private static uint Htonl(uint value)
        {
            // this branch is constant at JIT time and will be optimized out
            if (!BitConverter.IsLittleEndian)
                return value;

            var ptr = (byte*)&value;
            return (uint)(ptr[0] << 24 | ptr[1] << 16 | ptr[2] << 8 | ptr[3]);
        }

        private static ushort Htons(ushort value)
        {
            // this branch is constant at JIT time and will be optimized out
            if (!BitConverter.IsLittleEndian)
                return value;

            var ptr = (byte*)&value;
            return (ushort)(ptr[0] << 8 | ptr[1]);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (handle.IsAllocated)
                        handle.Free();

                    stream?.Close();
                    stream?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DataReader()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
