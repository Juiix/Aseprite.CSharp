# Aseprite.CSharp

**Aseprite.CSharp** is a lightweight C# library for reading `.ase` and `.aseprite` files — the native formats of [Aseprite](https://www.aseprite.org/) — and extracting their animation, layer, cel, palette, and metadata information. It supports asynchronous reading and optional cel decompression.

📄 **[ASE File Format Specification](https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md)**

## Features

* Asynchronously load `.aseprite` files.
* Read all core metadata: frames, layers, cels, palette, slices, tags, user data, and more.
* Optionally decompress cel image data during loading, or handle it manually.

## Usage

```csharp
using var stream = File.OpenRead("sprite.aseprite");
var aseFile = await AsepriteFile.ReadAsync(stream, decompressCels: true);
```

### Decompression

If `decompressCels` is set to `false`, compressed cel image data will remain in its original form. You can manually decompress it later using the provided methods:

```csharp
var decompressed = AsepriteFile.Decompress(compressedMemory);
// or
int bytesWritten = AsepriteFile.DecompressInto(compressedMemory, buffer, offset, length);
```

## API

```csharp
public sealed record AsepriteFile(
	AsepriteHeader Header,
	ReadOnlyMemory<AsepriteFrame> Frames,
	ReadOnlyMemory<AsepriteLayer> Layers,
	ReadOnlyMemory<ReadOnlyMemory<AsepriteCel?>> Cels,
	AsepriteColorProfile? ColorProfile,
	AsepriteExternalFiles? ExternalFiles,
	ReadOnlyMemory<AsepriteTag> Tags,
	ReadOnlyMemory<PaletteEntry> Palette,
	Dictionary<AsepriteObject, AsepriteUserData> UserDatas,
	ReadOnlyMemory<AsepriteSlice> Slices)
{
	public static Task<AsepriteFile> ReadAsync(Stream stream, bool decompressCels, CancellationToken cancellationToken = default);

	public static ReadOnlyMemory<byte> Decompress(ReadOnlyMemory<byte> memory);

	public static int DecompressInto(ReadOnlyMemory<byte> input, byte[] output, int offset, int length);
}
```

## License

[Apache 2.0](LICENSE)