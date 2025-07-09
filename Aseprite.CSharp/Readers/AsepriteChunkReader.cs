using System.Buffers;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Aseprite.CSharp.Readers;

internal static class AsepriteChunkReader
{
	/// <summary>
	/// Reads the "old palette" chunk (type 0x0004 – 24-bit RGB, ≤ 256 colors).
	/// </summary>
	public static Rgba[] ReadOldPaletteChunk1(ReadOnlySpan<byte> span)
	{
		var br = new BinaryReader(span);
		ushort packetCount = br.ReadWord();

		var colors = new List<Rgba>(256);
		int index = 0;

		for (int p = 0; p < packetCount; p++)
		{
			index += br.ReadByte();                     // skip entries
			int count = br.ReadByte();
			if (count == 0) count = 256;

			for (int i = 0; i < count; i++, index++)
			{
				byte r = br.ReadByte();
				byte g = br.ReadByte();
				byte b = br.ReadByte();
				EnsureCapacity(colors, index);
				colors[index] = new Rgba(r, g, b, 255);
			}
		}

		return [.. colors];
	}

	/// <summary>
	/// Reads the "old palette (6-bit)" chunk (type 0x0011 – RGB 0-63, ≤ 256 colors).
	/// Every channel is rescaled to 0-255.
	/// </summary>
	public static Rgba[] ReadOldPaletteChunk2(ReadOnlySpan<byte> span)
	{
		var br = new BinaryReader(span);
		ushort packetCount = br.ReadWord();

		var colors = new List<Rgba>(256);
		int index = 0;

		for (int p = 0; p < packetCount; p++)
		{
			index += br.ReadByte();                     // skip entries
			int count = br.ReadByte();
			if (count == 0) count = 256;

			for (int i = 0; i < count; i++, index++)
			{
				byte r6 = br.ReadByte();
				byte g6 = br.ReadByte();
				byte b6 = br.ReadByte();
				EnsureCapacity(colors, index);
				colors[index] = new Rgba(Scale6To8(r6),
										  Scale6To8(g6),
										  Scale6To8(b6),
										  255);
			}
		}

		return [.. colors];
	}

	/// <param name="layersHaveUuid">
	/// Pass <c>(header.Flags & SpriteFlags.LayersHaveUuid) != 0</c>.
	/// </param>
	public static AsepriteLayer ReadLayer(ReadOnlySpan<byte> span, bool layersHaveUuid)
	{
		var br = new BinaryReader(span);
		var flags = (LayerFlags)br.ReadWord();
		var type = (LayerType)br.ReadWord();
		ushort level = br.ReadWord();
		br.ReadWord();                 // default width  (ignored)
		br.ReadWord();                 // default height (ignored)
		var blend = (BlendMode)br.ReadWord();
		byte opacity = br.ReadByte();
		br.ReadBytes(3);               // future (zero)

		string name = br.ReadString();

		uint? tileset = null;
		if (type == LayerType.Tilemap)
			tileset = br.ReadDWord();

		Guid? uuid = null;
		if (layersHaveUuid)
			uuid = br.ReadUuid();

		return new AsepriteLayer(flags, type, level, blend,
								 opacity, name, tileset, uuid);
	}

	/// <summary>
	/// Reads a cel chunk (type 0x2005).  `dataLen` = <c>chunk.DataLength</c>.
	/// `depth` is the sprite’s color depth from the file header (8/16/32 bpp)
	/// so the reader can compute the raw-pixel size for type 0.
	/// </summary>
	public static AsepriteCel ReadCel(ReadOnlySpan<byte> span, ColorDepth depth, bool decompressCels)
	{
		var br = new BinaryReader(span);

		ushort layerIndex = br.ReadWord();
		short x = br.ReadShort();
		short y = br.ReadShort();
		byte opacity = br.ReadByte();
		var celType = (CelType)br.ReadWord();
		short zIndex = br.ReadShort();
		br.ReadBytes(5);                  // future (zero)

		CelContent content = celType switch
		{
			CelType.RawImage => ReadRawImage(ref br, depth),
			CelType.Linked => ReadLinked(ref br),
			CelType.CompressedImage => ReadCompressedImage(ref br, decompressCels),
			CelType.CompressedTilemap => ReadCompressedTilemap(ref br, decompressCels),
			_ => throw new InvalidDataException($"Unknown cel type {celType}")
		};

		return new AsepriteCel(layerIndex, x, y, opacity, celType, zIndex, content, null);
	}

	/// <summary>
	/// Reads the entire 36-byte payload of a Cel-Extra chunk.
	/// </summary>
	public static AsepriteCelExtra ReadCelExtra(ReadOnlySpan<byte> span)
	{
		var br = new BinaryReader(span);

		var flags = (CelExtraFlags)br.ReadDWord();
		float x = br.ReadFixed();
		float y = br.ReadFixed();
		float w = br.ReadFixed();
		float h = br.ReadFixed();

		br.ReadBytes(16);                      // reserved / future-use

		return new AsepriteCelExtra(flags, x, y, w, h);
	}

	public static AsepriteColorProfile ReadColorProfile(ReadOnlySpan<byte> span)
	{
		var br = new BinaryReader(span);

		var type = (ColorProfileType)br.ReadWord();
		var flags = (ColorProfileFlags)br.ReadWord();
		float gamma = br.ReadFixed();
		br.ReadBytes(8);                       // reserved

		ReadOnlyMemory<byte>? icc = null;
		if (type == ColorProfileType.ICC)
		{
			uint iccLen = br.ReadDWord();
			icc = br.ReadBytes(checked((int)iccLen)).ToArray();
		}

		return new AsepriteColorProfile(type, flags, gamma, icc);
	}

	/// <summary>
	/// Reads the payload of chunk <c>0x2018</c> and returns every entry.
	/// </summary>
	public static AsepriteExternalFiles ReadExternalFiles(ReadOnlySpan<byte> span)
	{
		// Create our little-endian BinaryReader on top of the span.
		var br = new BinaryReader(span.ToArray());

		uint entryCount = br.ReadDWord();
		br.ReadBytes(8);                       // reserved

		var list = new List<ExternalFileEntry>((int)entryCount);

		for (uint i = 0; i < entryCount; i++)
		{
			uint id = br.ReadDWord();
			var type = (ExternalFileType)br.ReadByte();
			br.ReadBytes(7);                   // reserved
			string name = br.ReadString();

			list.Add(new ExternalFileEntry(id, type, name));
		}

		return new AsepriteExternalFiles(list.ToArray());
	}

	/// <summary>Reads chunk <c>0x2017</c> from its raw payload.</summary>
	public static AsepriteTag[] ReadTags(ReadOnlySpan<byte> span)
	{
		var br = new BinaryReader(span.ToArray());      // uses our earlier struct

		ushort tagCount = br.ReadWord();
		br.ReadBytes(8);                                // reserved

		var list = new List<AsepriteTag>(tagCount);

		for (int i = 0; i < tagCount; i++)
		{
			ushort from = br.ReadWord();
			ushort to = br.ReadWord();
			var dir = (TagDirection)br.ReadByte();
			ushort rep = br.ReadWord();
			br.ReadBytes(6);                            // reserved

			byte r = br.ReadByte();
			byte g = br.ReadByte();
			byte b = br.ReadByte();
			br.ReadByte();                              // extra byte

			string name = br.ReadString();
			list.Add(new AsepriteTag(
				from, to, dir, rep, new Rgba(r, g, b, 255), name));
		}

		return [.. list];
	}

	/// <summary>Reads the payload of chunk <c>0x2019</c> (modern palette).</summary>
	public static AsepritePalette ReadPalette(ReadOnlySpan<byte> span)
	{
		var br = new BinaryReader(span.ToArray());

		uint newSize = br.ReadDWord();
		uint from = br.ReadDWord();
		uint to = br.ReadDWord();
		br.ReadBytes(8);                       // reserved

		int count = checked((int)(to - from + 1));
		var list = new List<PaletteEntry>(count);

		for (int i = 0; i < count; i++)
		{
			var flags = (PaletteEntryFlags)br.ReadWord();
			byte r = br.ReadByte();
			byte g = br.ReadByte();
			byte b = br.ReadByte();
			byte a = br.ReadByte();

			string? name = null;
			if ((flags & PaletteEntryFlags.HasName) != 0)
				name = br.ReadString();

			list.Add(new PaletteEntry(new Rgba(r, g, b, a), name));
		}

		return new AsepritePalette(newSize, from, to, list.ToArray());
	}

	/// <summary>Reads chunk <c>0x2020</c> from its raw payload.</summary>
	public static AsepriteUserData ReadUserData(ReadOnlySpan<byte> span)
	{
		var br = new BinaryReader(span.ToArray());

		var flags = (UserDataFlags)br.ReadDWord();

		string? text = null;
		if ((flags & UserDataFlags.HasText) != 0)
			text = br.ReadString();

		Rgba? color = null;
		if ((flags & UserDataFlags.HasColor) != 0)
			color = new Rgba(br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte());

		IReadOnlyList<PropertiesMap>? maps = null;
		if ((flags & UserDataFlags.HasProperties) != 0)
		{
			uint mapsBytes = br.ReadDWord();  // includes these 8 bytes
			uint mapCount = br.ReadDWord();

			var mapList = new List<PropertiesMap>((int)mapCount);
			for (uint m = 0; m < mapCount; m++)
			{
				uint mapKey = br.ReadDWord();
				uint propCnt = br.ReadDWord();
				var dict = ReadProperties(ref br, (int)propCnt);
				mapList.Add(new PropertiesMap(mapKey, dict));
			}
			maps = mapList;
		}

		return new AsepriteUserData(flags, text, color, maps);
	}

	/// <summary>Reads chunk <c>0x2022</c> from its payload.</summary>
	public static AsepriteSlice ReadSlice(ReadOnlySpan<byte> span)
	{
		var br = new BinaryReader(span.ToArray());   // our earlier BinaryReader struct

		uint keyCount = br.ReadDWord();
		var flags = (SliceFlags)br.ReadDWord();
		br.ReadDWord();                              // reserved
		string name = br.ReadString();

		var keys = new SliceKey[(int)keyCount];

		for (uint k = 0; k < keyCount; k++)
		{
			uint frame = br.ReadDWord();
			int x = br.ReadLong();
			int y = br.ReadLong();
			uint width = br.ReadDWord();
			uint height = br.ReadDWord();

			SliceCenter? center = null;
			if ((flags & SliceFlags.NinePatch) != 0)
			{
				int cx = br.ReadLong();
				int cy = br.ReadLong();
				uint cw = br.ReadDWord();
				uint ch = br.ReadDWord();
				center = new SliceCenter(cx, cy, cw, ch);
			}

			SlicePivot? pivot = null;
			if ((flags & SliceFlags.HasPivot) != 0)
			{
				int px = br.ReadLong();
				int py = br.ReadLong();
				pivot = new SlicePivot(px, py);
			}

			keys[k] = new SliceKey(frame, x, y, width, height, center, pivot);
		}

		return new AsepriteSlice(flags, name, keys);
	}


	// ───────────────── helpers ─────────────────
	private static byte Scale6To8(byte v6) => (byte)((v6 * 255 + 31) / 63);
	private static void EnsureCapacity(List<Rgba> list, int index)
	{
		while (list.Count <= index) list.Add(default);
	}

	// ─────── helpers for each cel subtype ────────
	private static RawCelContent ReadRawImage(ref BinaryReader br, ColorDepth depth)
	{
		ushort w = br.ReadWord();
		ushort h = br.ReadWord();

		int bytesPerPixel = depth switch
		{
			ColorDepth.Rgba32 => 4,
			ColorDepth.Grayscale16 => 2,
			ColorDepth.Indexed8 => 1,
			_ => throw new InvalidDataException($"Unsupported depth {depth}")
		};

		int bytes = w * h * bytesPerPixel;
		ReadOnlyMemory<byte> pixels = br.ReadBytes(bytes).ToArray();

		return new RawCelContent(w, h, pixels);
	}

	private static LinkedCelContent ReadLinked(ref BinaryReader br) =>
		new(br.ReadWord());

	private static CelContent ReadCompressedImage(ref BinaryReader br, bool decompress)
	{
		ushort w = br.ReadWord();
		ushort h = br.ReadWord();
		ReadOnlyMemory<byte> payload = br.ReadBytes(br.Remaining).ToArray();
		if (decompress) payload = AsepriteCompressionReader.Decompress(payload);
		return decompress
			? new RawCelContent(w, h, payload)
			: new CompressedImageCelContent(w, h, payload);
	}

	private static CelContent ReadCompressedTilemap(ref BinaryReader br, bool decompress)
	{
		ushort w = br.ReadWord();
		ushort h = br.ReadWord();
		ushort bitsPerTile = br.ReadWord();
		uint idMask = br.ReadDWord();
		uint xFlipMask = br.ReadDWord();
		uint yFlipMask = br.ReadDWord();
		uint diagFlipMask = br.ReadDWord();
		br.ReadBytes(10);                   // reserved

		ReadOnlyMemory<byte> payload = br.ReadBytes(br.Remaining).ToArray();
		if (decompress) payload = AsepriteCompressionReader.Decompress(payload);
		return decompress
			? new TilemapCelContent(
				w, h, bitsPerTile, idMask,
				xFlipMask, yFlipMask, diagFlipMask, payload)
			: new CompressedTilemapCelContent(
				w, h, bitsPerTile, idMask,
				xFlipMask, yFlipMask, diagFlipMask, payload);
	}

	// ────────────────  recursive helpers  ────────────────
	private static Dictionary<string, UserDataProperty> ReadProperties(ref BinaryReader br, int n)
	{
		var dict = new Dictionary<string, UserDataProperty>(n, StringComparer.Ordinal);
		for (int i = 0; i < n; i++)
		{
			string name = br.ReadString();
			var type = (PropertyType)br.ReadWord();
			object val = ReadValue(ref br, type);
			dict[name] = new UserDataProperty(type, val);
		}
		return dict;
	}

	private static object ReadValue(ref BinaryReader br, PropertyType type) => type switch
	{
		PropertyType.Bool => br.ReadByte() != 0,
		PropertyType.Int8 => unchecked((sbyte)br.ReadByte()),
		PropertyType.UInt8 => br.ReadByte(),
		PropertyType.Int16 => br.ReadShort(),
		PropertyType.UInt16 => br.ReadWord(),
		PropertyType.Int32 => br.ReadLong(),
		PropertyType.UInt32 => br.ReadDWord(),
		PropertyType.Int64 => br.ReadLong64(),
		PropertyType.UInt64 => br.ReadQWord(),
		PropertyType.Fixed => br.ReadFixed(),
		PropertyType.Float => br.ReadFloat(),
		PropertyType.Double => br.ReadDouble(),
		PropertyType.String => br.ReadString(),
		PropertyType.Point => br.ReadPoint(),
		PropertyType.Size => br.ReadSize(),
		PropertyType.Rect => br.ReadRect(),
		PropertyType.Uuid => br.ReadUuid(),
		PropertyType.Vector => ReadVector(ref br),
		PropertyType.Map => ReadNestedMap(ref br),
		_ => throw new InvalidDataException($"Unknown property type {type}")
	};

	private static List<UserDataProperty> ReadVector(ref BinaryReader br)
	{
		uint elemCount = br.ReadDWord();
		var elemType = (PropertyType)br.ReadWord();

		var list = new List<UserDataProperty>((int)elemCount);

		if (elemType == 0) // heterogeneous
		{
			for (uint i = 0; i < elemCount; i++)
			{
				var t = (PropertyType)br.ReadWord();
				object v = ReadValue(ref br, t);
				list.Add(new UserDataProperty(t, v));
			}
		}
		else               // homogeneous
		{
			for (uint i = 0; i < elemCount; i++)
			{
				object v = ReadValue(ref br, elemType);
				list.Add(new UserDataProperty(elemType, v));
			}
		}
		return list;
	}

	private static Dictionary<string, UserDataProperty> ReadNestedMap(ref BinaryReader br)
	{
		uint nestedCount = br.ReadDWord();
		return ReadProperties(ref br, (int)nestedCount);
	}
}
