namespace Aseprite.CSharp;

// ────────────────────  simple value types  ────────────────────
public readonly record struct Point(int X, int Y);
public readonly record struct Size(int Width, int Height);
public readonly record struct Rect(Point Origin, Size Size);
public readonly record struct Rgba(byte R, byte G, byte B, byte A);
public readonly record struct Gray(byte Value, byte Alpha);
public readonly record struct Tile(uint Value);