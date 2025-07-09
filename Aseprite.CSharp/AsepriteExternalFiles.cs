namespace Aseprite.CSharp;

public sealed record AsepriteExternalFiles(ExternalFileEntry[] Entries) : AsepriteObject;

public sealed record ExternalFileEntry
(
	uint Id,
	ExternalFileType Type,
	string NameOrId          // file-name or extension-ID string
);