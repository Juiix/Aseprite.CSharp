using System.Buffers.Binary;
using System.Text;

namespace Aseprite.CSharp;

internal ref struct BinaryReader(ReadOnlySpan<byte> span)
{
	private ReadOnlySpan<byte> _span = span;
	private int _pos = 0;

	public readonly bool EndOfStream => _pos >= _span.Length;
	public readonly int Remaining => _span.Length - _pos;

	// ────────────  primitives  ────────────
	public byte ReadByte() => ReadSlice(1)[0];

	public ushort ReadWord() => BinaryPrimitives.ReadUInt16LittleEndian(ReadSlice(2));
	public short ReadShort() => BinaryPrimitives.ReadInt16LittleEndian(ReadSlice(2));

	public uint ReadDWord() => BinaryPrimitives.ReadUInt32LittleEndian(ReadSlice(4));
	public int ReadLong() => BinaryPrimitives.ReadInt32LittleEndian(ReadSlice(4));

	/// <summary>Read a 16.16 fixed-point value as <see cref="float"/>.</summary>
	public float ReadFixed()
	{
		int raw = ReadLong();
		return raw / 65536f;
	}

	public float ReadFloat() => BitConverter.UInt32BitsToSingle(ReadDWord());
	public double ReadDouble() => BitConverter.Int64BitsToDouble((long)ReadQWord());

	public ulong ReadQWord() => BinaryPrimitives.ReadUInt64LittleEndian(ReadSlice(8));
	public long ReadLong64() => BinaryPrimitives.ReadInt64LittleEndian(ReadSlice(8));

	public ReadOnlySpan<byte> ReadBytes(int count) => ReadSlice(count);

	// ────────────  composite formats  ────────────
	public string ReadString()
	{
		ushort byteLen = ReadWord();
		var bytes = ReadSlice(byteLen);
		return Encoding.UTF8.GetString(bytes);
	}

	public Point ReadPoint() => new(ReadLong(), ReadLong());
	public Size ReadSize() => new(ReadLong(), ReadLong());
	public Rect ReadRect() => new(ReadPoint(), ReadSize());

	// ────────────  pixel helpers  ────────────
	public Rgba ReadRgbaPixel() => new(ReadByte(), ReadByte(), ReadByte(), ReadByte());
	public Gray ReadGrayPixel() => new(ReadByte(), ReadByte());
	public byte ReadIndexedPixel() => ReadByte();

	// ────────────  tiles & UUID  ────────────
	public byte ReadTile8() => ReadByte();
	public ushort ReadTile16() => ReadWord();
	public uint ReadTile32() => ReadDWord();

	public Guid ReadUuid()
	{
		ReadOnlySpan<byte> bytes = ReadSlice(16);
		return new Guid(bytes);
	}

	// ────────────  internal helpers  ────────────
	private ReadOnlySpan<byte> ReadSlice(int length)
	{
		if (_pos + length > _span.Length)
			throw new InvalidOperationException("Unexpected end of data.");

		ReadOnlySpan<byte> slice = _span.Slice(_pos, length);
		_pos += length;
		return slice;
	}
}
