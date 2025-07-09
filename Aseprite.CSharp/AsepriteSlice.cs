namespace Aseprite.CSharp;

public readonly record struct SliceCenter(int X, int Y, uint Width, uint Height);
public readonly record struct SlicePivot(int X, int Y);

public sealed record SliceKey
(
	uint Frame,
	int X,
	int Y,
	uint Width,
	uint Height,
	SliceCenter? Center,
	SlicePivot? Pivot
);

public sealed record AsepriteSlice
(
	SliceFlags Flags,
	string Name,
	ReadOnlyMemory<SliceKey> Keys
) : AsepriteObject;