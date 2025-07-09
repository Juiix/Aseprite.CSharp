namespace Aseprite.CSharp;

public sealed record AsepriteColorProfile
(
	ColorProfileType Type,
	ColorProfileFlags Flags,
	float FixedGamma,        // 1.0 = linear
	ReadOnlyMemory<byte>? IccProfile     // null unless Type == ICC
) : AsepriteObject;