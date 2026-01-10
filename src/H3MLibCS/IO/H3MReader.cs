using System.IO.Compression;
using System.Text;

namespace H3M.IO;

public class H3MReader : IDisposable
{
    private Stream _stream; 
    private bool _disposed;

    public long Position 
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }
    
    public long Length => _stream.Length;

    static H3MReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public H3MReader(Stream input)
    {
        if (IsGZipStream(input))
        {
            var memStream = new MemoryStream();
            using var gzip = new GZipStream(input, CompressionMode.Decompress, leaveOpen: true);
            gzip.CopyTo(memStream);
            memStream.Position = 0;
            _stream = memStream;
        }
        else
        {
            if (input.CanSeek)
            {
                _stream = input;
            }
            else
            {
                var memStream = new MemoryStream();
                input.CopyTo(memStream);
                memStream.Position = 0;
                _stream = memStream;
            }
        }
    }

    private static bool IsGZipStream(Stream input)
    {
        if (!input.CanSeek) return false;
        long p = input.Position;
        int b1 = input.ReadByte();
        int b2 = input.ReadByte();
        input.Position = p;
        return b1 == 0x1F && b2 == 0x8B;
    }

    public byte ReadByte() 
    {
        int b = _stream.ReadByte();
        if (b == -1) throw new EndOfStreamException();
        return (byte)b;
    }
    
    public byte[] ReadBytes(int count) 
    {
        byte[] buffer = new byte[count];
        _stream.ReadExactly(buffer, 0, count);
        return buffer;
    }
    
    public bool ReadBool() => ReadByte() != 0;
    
    public ushort ReadUInt16() 
    {
        Span<byte> b = stackalloc byte[2];
        _stream.ReadExactly(b);
        return (ushort)(b[0] | (b[1] << 8));
    }
    
    public uint ReadUInt32() 
    {
        Span<byte> b = stackalloc byte[4];
        _stream.ReadExactly(b);
        return (uint)(b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24));
    }
    
    public int ReadInt32() 
    {
        Span<byte> b = stackalloc byte[4];
        _stream.ReadExactly(b);
        return (int)(b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24));
    }

    public string ReadString()
    {
        uint length = ReadUInt32();
        if (length == 0 || length == 0xFFFFFFFF) return string.Empty;
        if (length > 0x100000) 
             throw new InvalidDataException($"String length too large: {length} at position {Position - 4}");

        byte[] bytes = ReadBytes((int)length);
        return Encoding.GetEncoding(1251).GetString(bytes); 
    }

    public void Skip(int count)
    {
        if (_stream.Position + count > _stream.Length)
            throw new EndOfStreamException("Attempted to skip beyond end of stream.");
        _stream.Seek(count, SeekOrigin.Current);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _stream.Dispose();
            _disposed = true;
        }
    }
}
