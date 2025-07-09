namespace Aseprite.CSharp;

public sealed record AsepriteLayer
(
	LayerFlags Flags,
	LayerType Type,
	ushort ChildLevel,
	BlendMode Blend,
	byte Opacity,
	string Name,
	uint? TilesetIndex,
	Guid? Uuid
) : AsepriteObject;