using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace H3M.IO
{
    public class H3MContext : IDisposable
    {
        private readonly Stream _baseStream;
        private readonly BinaryReader _reader;

        public H3MContext(Stream stream)
        {
            if (IsGZip(stream))
            {
                _baseStream = new GZipStream(stream, CompressionMode.Decompress);
            }
            else
            {
                _baseStream = stream;
            }
            _reader = new BinaryReader(_baseStream, Encoding.GetEncoding(1251));
        }

        private static bool IsGZip(Stream stream)
        {
            if (!stream.CanSeek) return false;
            byte[] header = new byte[2];
            int bytesRead = stream.Read(header, 0, 2);
            stream.Seek(0, SeekOrigin.Begin);
            return bytesRead == 2 && header[0] == 0x1F && header[1] == 0x8B;
        }

        public uint ReadUInt32() => _reader.ReadUInt32();
        public int ReadInt32() => _reader.ReadInt32();
        public byte ReadByte() => _reader.ReadByte();
        public byte[] ReadBytes(int count) => _reader.ReadBytes(count);

        public string ReadH3String()
        {
            uint length = ReadUInt32();
            if (length == 0 || length > 1024 * 1024) return string.Empty;
            byte[] bytes = _reader.ReadBytes((int)length);
            return Encoding.GetEncoding(1251).GetString(bytes).TrimEnd('\0');
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _baseStream?.Dispose();
        }
    }
}
