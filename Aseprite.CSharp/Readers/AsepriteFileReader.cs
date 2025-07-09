using System.Buffers;

namespace Aseprite.CSharp.Readers;

internal static class AsepriteFileReader
{
	private const int BufferSize = 4096;

	public static async Task<AsepriteFile> ReadAsync(Stream stream, bool decompressCels, CancellationToken cancellationToken = default)
	{
		var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
		try
		{
			// header
			var readBuffer = buffer.AsMemory(0, 128);
			await stream.ReadExactlyAsync(readBuffer, cancellationToken).ConfigureAwait(false);
			var header = AsepriteHeaderReader.Read(readBuffer.Span);
			var frames = new AsepriteFrame[header.Frames];
			List<AsepriteLayer> layers = [];
			List<ReadOnlyMemory<AsepriteCel?>> cels = [];
			AsepriteColorProfile? colorProfile = null;
			AsepriteExternalFiles? externalFiles = null;
			AsepriteTag[] tags = [];
			List<PaletteEntry> palette = [];
			Dictionary<AsepriteObject, AsepriteUserData> userDatas = [];
			List<AsepriteSlice> slices = [];
			List<AsepriteCel?> frameCels = [];

			AsepriteObject? lastObject = null;

			// frames
			for (int i = 0; i < frames.Length; i++)
			{
				readBuffer = buffer.AsMemory(0, 16);
				await stream.ReadExactlyAsync(readBuffer, cancellationToken).ConfigureAwait(false);
				var frame = AsepriteFrameReader.Read(readBuffer.Span);
				frames[i] = frame;
				lastObject = frame;
				int tagIndex = 0;

				// chunks
				for (int j = 0; j < frame.ChunkCount; j++)
				{
					// chunk header
					readBuffer = buffer.AsMemory(0, 6);
					await stream.ReadExactlyAsync(readBuffer, cancellationToken).ConfigureAwait(false);
					var chunkHeader = AsepriteChunkHeaderReader.Read(readBuffer.Span);

					bool readNewPalette = false;

					// chunk bytes
					var tempChunkBuffer = chunkHeader.DataLength > buffer.Length ? ArrayPool<byte>.Shared.Rent(chunkHeader.DataLength) : null;
					try
					{
						var chunkBuffer = tempChunkBuffer ?? buffer;
						readBuffer = chunkBuffer.AsMemory(0, chunkHeader.DataLength);
						await stream.ReadExactlyAsync(readBuffer, cancellationToken).ConfigureAwait(false);

						switch (chunkHeader.Type)
						{
							case ChunkType.OldPaletteChunk1:
								if (!readNewPalette)
								{
									var colors = AsepriteChunkReader.ReadOldPaletteChunk1(readBuffer.Span);
									palette = colors.Select(x => new PaletteEntry(x, null)).ToList();
								}
								break;
							case ChunkType.OldPaletteChunk2:
								if (!readNewPalette)
								{
									var colors = AsepriteChunkReader.ReadOldPaletteChunk2(readBuffer.Span);
									palette = colors.Select(x => new PaletteEntry(x, null)).ToList();
								}
								break;
							case ChunkType.Layer:
								var layer = AsepriteChunkReader.ReadLayer(readBuffer.Span, (header.Flags & AsepriteFileFlags.LayersHaveUuid) == AsepriteFileFlags.LayersHaveUuid);
								layers.Add(layer);
								lastObject = layer;
								break;
							case ChunkType.Cel:
								var cel = AsepriteChunkReader.ReadCel(readBuffer.Span, header.Depth, decompressCels);
								while (frameCels.Count <= cel.LayerIndex) frameCels.Add(null);
								frameCels[cel.LayerIndex] = cel;
								lastObject = cel;
								break;
							case ChunkType.CelExtra:
								var celExtra = AsepriteChunkReader.ReadCelExtra(readBuffer.Span);
								if (frameCels.Count > 0)
								{
									var prevCel = frameCels[^1];
									if (prevCel != null)
										frameCels[^1] = prevCel with { Extra = celExtra };
								}
								lastObject = celExtra;
								break;
							case ChunkType.ColorProfile:
								colorProfile = AsepriteChunkReader.ReadColorProfile(readBuffer.Span);
								lastObject = colorProfile;
								break;
							case ChunkType.ExternalFiles:
								externalFiles = AsepriteChunkReader.ReadExternalFiles(readBuffer.Span);
								lastObject = externalFiles;
								break;
							case ChunkType.Tags:
								tags = AsepriteChunkReader.ReadTags(readBuffer.Span);
								lastObject = tags.FirstOrDefault();
								break;
							case ChunkType.Palette:
								readNewPalette = true;
								var palettePart = AsepriteChunkReader.ReadPalette(readBuffer.Span);
								while (palette.Count < palettePart.NewSize) palette.Add(default);
								for (uint k = palettePart.FromIndex; k <= palettePart.ToIndex; k++)
									palette[(int)k] = palettePart.Entries[k - palettePart.FromIndex];
								lastObject = palettePart;
								break;
							case ChunkType.UserData:
								var userData = AsepriteChunkReader.ReadUserData(readBuffer.Span);
								if (lastObject is null)
								{
									tagIndex = 0;
									break;
								}

								if (lastObject is not AsepriteTag)
								{
									tagIndex = 0;
									userDatas[lastObject] = userData;
								}
								else if (tagIndex < tags.Length)
								{
									userDatas[tags[tagIndex++]] = userData;
								}
								break;
							case ChunkType.Slice:
								var slice = AsepriteChunkReader.ReadSlice(readBuffer.Span);
								slices.Add(slice);
								lastObject = slice;
								break;

						}
					}
					finally
					{
						if (tempChunkBuffer != null)
						{
							ArrayPool<byte>.Shared.Return(tempChunkBuffer);
						}
					}
				}

				cels.Add(frameCels.ToArray());
				frameCels.Clear();
			}

			if (header.TransparentIndex < palette.Count)
				palette[header.TransparentIndex] = default;

			return new AsepriteFile(
				header, frames, layers.ToArray(),
				cels.ToArray(), colorProfile, externalFiles,
				tags, palette.ToArray(), userDatas, slices.ToArray());
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}
}
