using System;

namespace RoosterControl.Infrastructure.Assets;

/// <summary>Describes a set of mod assets keyed by an enum.</summary>
internal sealed record AssetData(Type enumAssetType, Enum enumAssetFolder, ModAssetHandler.FileExtension fileExt, string gamePath);
