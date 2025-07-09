namespace Aseprite.CSharp;

[Flags]
public enum AsepriteFileFlags
{
	LayerOpacityValid = 1,
	GroupBlendValid = 2,
	LayersHaveUuid = 4
}

internal enum ChunkType : ushort
{
	OldPaletteChunk1 = 0x0004,
	OldPaletteChunk2 = 0x0011,
	Layer = 0x2004,
	Cel = 0x2005,
	CelExtra = 0x2006,
	ColorProfile = 0x2007,
	ExternalFiles = 0x2008,
	Tags = 0x2018,
	Palette = 0x2019,
	UserData = 0x2020,
	Slice = 0x2022,
	Tileset = 0x2023,
}

public enum ColorDepth : ushort
{
	Indexed8 = 8,
	Grayscale16 = 16,
	Rgba32 = 32
}

[Flags]
public enum LayerFlags : ushort
{
	Visible = 1,
	Editable = 2,
	LockMovement = 4,
	Background = 8,
	PreferLinkedCels = 16,
	GroupCollapsed = 32,
	ReferenceLayer = 64
}

public enum LayerType : ushort { Normal = 0, Group = 1, Tilemap = 2 }

public enum BlendMode : ushort
{
	Normal = 0, Multiply = 1, Screen = 2, Overlay = 3,
	Darken = 4, Lighten = 5, ColorDodge = 6, ColorBurn = 7,
	HardLight = 8, SoftLight = 9, Difference = 10, Exclusion = 11,
	Hue = 12, Saturation = 13, Color = 14, Luminosity = 15,
	Addition = 16, Subtract = 17, Divide = 18
}

public enum CelType : ushort
{
	RawImage = 0,
	Linked = 1,
	CompressedImage = 2,
	CompressedTilemap = 3
}

[Flags]
public enum CelExtraFlags : uint
{
	PreciseBounds = 1      // bit-0 → the four FIXED bounds are valid
}

public enum ColorProfileType : ushort { None = 0, sRGB = 1, ICC = 2 }

[Flags]
public enum ColorProfileFlags : ushort
{
	UseFixedGamma = 1   // bit-0 → honor the FixedGamma field
}

public enum ExternalFileType : byte
{
	ExternalPalette = 0,
	ExternalTileset = 1,
	ExtensionPropertyName = 2,
	ExtensionTileManagement = 3
}

public enum TagDirection : byte
{
	Forward = 0,
	Reverse = 1,
	PingPong = 2,
	PingPongReverse = 3
}

[Flags]
public enum PaletteEntryFlags : ushort { HasName = 1 }

[Flags] public enum UserDataFlags : uint { HasText = 1, HasColor = 2, HasProperties = 4 }

public enum PropertyType : ushort
{
	Bool = 0x0001, Int8 = 0x0002, UInt8 = 0x0003, Int16 = 0x0004,
	UInt16 = 0x0005, Int32 = 0x0006, UInt32 = 0x0007, Int64 = 0x0008,
	UInt64 = 0x0009, Fixed = 0x000A, Float = 0x000B, Double = 0x000C,
	String = 0x000D, Point = 0x000E, Size = 0x000F, Rect = 0x0010,
	Vector = 0x0011, Map = 0x0012, Uuid = 0x0013
}

[Flags]
public enum SliceFlags : uint
{
	NinePatch = 1,
	HasPivot = 2
}