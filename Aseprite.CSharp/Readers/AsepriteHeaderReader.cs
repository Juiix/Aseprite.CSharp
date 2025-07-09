namespace Aseprite.CSharp.Readers;

internal static class AsepriteHeaderReader
{
	/// <summary>Reads and returns the header. Leaves the reader positioned
	/// at the byte immediately following the 84 reserved bytes.</summary>
	public static AsepriteHeader Read(ReadOnlySpan<byte> span)
	{
		var br = new BinaryReader(span);
		uint fileSize = br.ReadDWord();
		ushort magic = br.ReadWord();           // 0xA5E0
		ushort frames = br.ReadWord();
		ushort width = br.ReadWord();
		ushort height = br.ReadWord();
		var depth = (ColorDepth)br.ReadWord();
		var flags = (AsepriteFileFlags)br.ReadDWord();
		ushort speed = br.ReadWord();           // deprecated
		uint reserved1 = br.ReadDWord();          // 0
		uint reserved2 = br.ReadDWord();          // 0
		byte transparent = br.ReadByte();           // indexed sprites only
		br.ReadBytes(3);                              // skip padding
		ushort colorCount = br.ReadWord();           // 0 → 256
		byte pxWidth = br.ReadByte();
		byte pxHeight = br.ReadByte();
		short gridX = br.ReadShort();
		short gridY = br.ReadShort();
		ushort gridW = br.ReadWord();
		ushort gridH = br.ReadWord();
		br.ReadBytes(84);                             // future-use bytes

		return new AsepriteHeader(
			fileSize, magic, frames, width, height, depth, flags, speed,
			reserved1, reserved2, transparent, colorCount,
			pxWidth, pxHeight, gridX, gridY, gridW, gridH);
	}
}