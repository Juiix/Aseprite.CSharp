namespace Aseprite.CSharp;

public sealed record UserDataProperty(PropertyType Type, object Value);

public sealed record PropertiesMap
(
	uint Key,        // 0 = user, else external-file entry-id
	IReadOnlyDictionary<string, UserDataProperty> Properties
);

public sealed record AsepriteUserData
(
	UserDataFlags Flags,
	string? Text,
	Rgba? Color,
	IReadOnlyList<PropertiesMap>? PropertyMaps
);