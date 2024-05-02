using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace CryRcon;

public static class StreamHelper
{
    delegate T ReadOnlySpanFunc<TArg, T>(ReadOnlySpan<TArg> span);

    static int GetByteCount(this string s)
        => Encoding.ASCII.GetByteCount(s);

    static T ReadPrimitive<T>(this Stream s, ReadOnlySpanFunc<byte, T> func)
    {
        Span<byte> buf = stackalloc byte[Unsafe.SizeOf<T>()];
        s.ReadExactly(buf);
        return func(buf);
    }

    static void WritePrimitive<T>(this Stream s, T value, SpanAction<byte, T> func)
    {
        Span<byte> buf = stackalloc byte[Unsafe.SizeOf<T>()];
        func(buf, value);
        s.Write(buf);
    }

    public static byte[] ReadUInt8Array(this Stream s, int size)
    {
        var buf = new byte[size];
        s.ReadExactly(buf);
        return buf;
    }

    public static byte ReadUInt8(this Stream s)
    {
        var b = s.ReadByte();

        if (b == -1)
            throw new EndOfStreamException();

        return (byte)b;
    }

    public static uint ReadUInt32BE(this Stream s)
        => s.ReadPrimitive(BinaryPrimitives.ReadUInt32BigEndian);
    
    public static int ReadInt32LE(this Stream s)
        => s.ReadPrimitive(BinaryPrimitives.ReadInt32LittleEndian);

    public static uint ReadUInt32LE(this Stream s)
        => s.ReadPrimitive(BinaryPrimitives.ReadUInt32LittleEndian);

    public static void WriteUInt32BE(this Stream s, uint value)
        => s.WritePrimitive(value, BinaryPrimitives.WriteUInt32BigEndian);

    public static void WriteUInt8(this Stream s, byte value)
        => s.WriteByte(value);

    public static void WriteString(this Stream s, string value, int size = 256)
    {
        var buf = new byte[Math.Max(size, value.GetByteCount())];
        Encoding.ASCII.GetBytes(value, buf);
        s.Write(buf, 0, buf.Length);
    }
}
