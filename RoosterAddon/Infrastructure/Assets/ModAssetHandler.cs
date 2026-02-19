using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace RoosterAddon.Infrastructure.Assets;

/// <summary>Registers and serves mod textures under <c>Mods/&lt;UniqueId&gt;/*</c>.</summary>
internal sealed class ModAssetHandler
{
    internal enum FileExtension
    {
        Png
    }

    internal enum AssetPathType
    {
        GamePath,
        ModFolderPath
    }

    private readonly string _uniqueId;
    private readonly IMonitor _monitor;

    private readonly List<AssetData> _assetSets = new();

    // Base asset name (no locale) -> mod file path (relative to mod folder).
    private readonly Dictionary<string, string> _assetNameToFile =
        new(StringComparer.OrdinalIgnoreCase);

    // (enum type, enum name) -> (game path, mod file path)
    private readonly Dictionary<(Type EnumType, string Name), (string Game, string File)> _byEnum =
        new();

    public ModAssetHandler(string uniqueId, IMonitor monitor)
    {
        _uniqueId = uniqueId ?? throw new ArgumentNullException(nameof(uniqueId));
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    }

    public void AddAssets(IEnumerable<AssetData> assets)
    {
        if (assets is null) throw new ArgumentNullException(nameof(assets));
        _assetSets.AddRange(assets);
    }

    public void SetUpAssets()
    {
        _assetNameToFile.Clear();
        _byEnum.Clear();

        foreach (var set in _assetSets)
        {
            if (!set.enumAssetType.IsEnum)
                throw new InvalidOperationException($"EnumAssetType must be an enum: {set.enumAssetType}");

            var folderName = set.enumAssetFolder.ToString() ?? string.Empty;
            var ext = ToExtension(set.fileExt);

            foreach (var name in Enum.GetNames(set.enumAssetType))
            {
                var gamePath = $"{set.gamePath}/{folderName}/{name}";
                var modFilePath = Path.Combine("assets", folderName, $"{name}.{ext}");

                _assetNameToFile[gamePath] = modFilePath;
                _byEnum[(set.enumAssetType, name)] = (gamePath, modFilePath);
            }
        }
    }

    public string GetAssetPath<TEnum>(TEnum asset, AssetPathType type)
        where TEnum : struct, Enum
    {
        var key = (typeof(TEnum), asset.ToString());
        if (!_byEnum.TryGetValue(key, out var paths))
            throw new KeyNotFoundException($"No asset registered for {typeof(TEnum).Name}.{asset}.");

        return type == AssetPathType.GamePath ? paths.Game : paths.File;
    }

    public void LoadModAssets(AssetRequestedEventArgs e)
    {
        if (e is null) throw new ArgumentNullException(nameof(e));

        var baseName = e.NameWithoutLocale.BaseName;
        if (!_assetNameToFile.TryGetValue(baseName, out var modFilePath))
            return;

        e.LoadFromModFile<Texture2D>(modFilePath, AssetLoadPriority.Exclusive);
    }

    private static string ToExtension(FileExtension ext) =>
        ext switch
        {
            FileExtension.Png => "png",
            _ => throw new ArgumentOutOfRangeException(nameof(ext), ext, null)
        };
}
