using System;
using System.Collections.Concurrent;
using System.Linq;
using LetMeHatch.Core.ClonePipeline;
using LetMeHatch.Core.Cloning;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Infrastructure.Content.Loaders;

/// <summary>
/// Dynamically generates per-clone 16x16 textures under <c>Mods/&lt;UniqueId&gt;/Objects/Clones/&lt;cloneId&gt;</c>.
/// </summary>
public sealed class DynamicCloneObjectTextureLoader : AssetLoaderBase
{
    private readonly string _prefixLower;
    private readonly int _prefixLen;

    private static readonly ConcurrentDictionary<string, Texture2D> Cache = new(StringComparer.Ordinal);

    public DynamicCloneObjectTextureLoader(string modUniqueId)
        : base(AssetLoadPriority.Exclusive)
    {
        if (string.IsNullOrWhiteSpace(modUniqueId))
            throw new ArgumentException("Mod unique id must be non-empty.", nameof(modUniqueId));

        _prefixLower = $"mods/{modUniqueId.ToLowerInvariant()}/objects/clones/";
        _prefixLen = _prefixLower.Length;
    }

    public override string DebugHandles => "Mods/<UniqueId>/Objects/Clones/<cloneId>";

    public override string DebugGates => "assetNameLower starts with mods/<uniqueid>/objects/clones/.";

    public override bool CanLoad(string assetNameLower, AssetRequestedEventArgs e)
        => assetNameLower.StartsWith(_prefixLower, StringComparison.Ordinal);

    public override void Load(string assetNameLower, AssetRequestedEventArgs e)
    {
        var fullName = e.NameWithoutLocale.BaseName;
        if (fullName.Length <= _prefixLen)
            return;

        var cloneId = fullName.Substring(_prefixLen);

        e.LoadFrom(
            () => Cache.GetOrAdd(cloneId, static id => BuildTexture(id)),
            AssetLoadPriority.Exclusive
        );
    }

    public static void ClearCache() => Cache.Clear();

    public static int CacheCount => Cache.Count;

    public static string[] GetCacheKeys(int maxKeys)
    {
        if (maxKeys <= 0) return Array.Empty<string>();
        return Cache.Keys.Take(maxKeys).ToArray();
    }

    private static Texture2D BuildTexture(string cloneId)
    {
        Game1.objectData.TryGetValue(cloneId, out var cloneData);

        if (!CloneIdService.TryParseCloneId(cloneId, out var animalKey, out var originalItemId, out _))
            throw new InvalidOperationException($"Not a recognized clone id: {cloneId}");

        if (!Game1.objectData.TryGetValue(originalItemId, out var originalData))
            throw new InvalidOperationException($"Original object not found: {originalItemId} (from clone {cloneId})");

        var sourceTexture = Game1.content.Load<Texture2D>(originalData.Texture);
        var pixels = CropSprite(sourceTexture, originalData.SpriteIndex);

        // If the flag is disabled, return the original sprite colors (still served through the dynamic asset path).
        if (!ModEntry.Config.UseDynamicCloneTextures)
            return ToTexture(pixels);

        var mode = ModEntry.Config.DynamicCloneTintMode;
        if (mode == DynamicCloneTintMode.None)
            return ToTexture(pixels);

        var tint = ResolveTint(cloneId, animalKey, originalItemId, cloneData, originalData);
        ApplyTintInPlace(pixels, tint, mode);
        return ToTexture(pixels);
    }

    private static Texture2D ToTexture(Color[] pixels)
    {
        var tex = new Texture2D(Game1.graphics.GraphicsDevice, 16, 16);
        tex.SetData(pixels);
        return tex;
    }

    private static Color ResolveTint(
        string cloneId,
        string animalKey,
        string originalItemId,
        ObjectData? cloneData,
        ObjectData originalData)
        => ResolveTintWithSource(cloneId, animalKey, originalItemId, cloneData, originalData).Tint;

    internal static (Color Tint, string Source) ResolveTintWithSource(
        string cloneId,
        string animalKey,
        string originalItemId,
        ObjectData? cloneData,
        ObjectData originalData)
    {
        // 1) Config override by original object id.
        if (ModEntry.Config.EggColors_ColorObject.TryGetValue(originalItemId, out var cfgColor))
            return (cfgColor, "config.override");

        // 2) Saved per-animal mapping (legacy LMH behavior).
        var saved = ModEntry.CurrentAssetManager?.SavedColors;
        if (saved is not null
            && saved.ItemColors.TryGetValue(animalKey, out var savedHex)
            && TryParseHexColor(savedHex, out var savedColor))
        {
            return (savedColor, "save.per_animal");
        }

        // 3) Clone custom field hex.
        var hex = TryGetCloneHexColor(cloneData);
        if (TryParseHexColor(hex, out var parsed))
            return (parsed, "clone.custom_field");

        // 4) Context tags fallback.
        var tags = cloneData?.ContextTags ?? originalData.ContextTags;
        var resolver = new ColorResolver();
        var resolved = resolver.Resolve(new ColorResolutionContext(Key: cloneId, SeedKey: cloneId, ContextTags: tags));
        return (resolved, "context_tags");
    }

    private static string TryGetCloneHexColor(ObjectData? cloneData)
    {
        if (cloneData?.CustomFields is null)
            return string.Empty;

        return cloneData.CustomFields.TryGetValue("ClonedColor", out var v) ? (v ?? string.Empty) : string.Empty;
    }

    private static bool TryParseHexColor(string hex, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        var h = hex.Trim();
        if (h.StartsWith("#", StringComparison.Ordinal))
            h = h.Substring(1);

        if (h.Length != 6)
            return false;

        if (!int.TryParse(h.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var r))
            return false;
        if (!int.TryParse(h.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g))
            return false;
        if (!int.TryParse(h.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
            return false;

        color = new Color(r, g, b);
        return true;
    }

    private static Color[] CropSprite(Texture2D texture, int spriteIndex)
    {
        const int tileSize = 16;
        var columns = Math.Max(1, texture.Width / tileSize);
        var x = (spriteIndex % columns) * tileSize;
        var y = (spriteIndex / columns) * tileSize;

        var data = new Color[tileSize * tileSize];
        texture.GetData(0, new Rectangle(x, y, tileSize, tileSize), data, 0, data.Length);
        return data;
    }

    private static void ApplyTintInPlace(Color[] pixels, Color tint, DynamicCloneTintMode mode)
    {
        switch (mode)
        {
            case DynamicCloneTintMode.Grayscale:
                ApplyGrayscaleTintInPlace(pixels, tint);
                return;

            case DynamicCloneTintMode.Multiply:
                ApplyMultiplyTintInPlace(pixels, tint);
                return;

            case DynamicCloneTintMode.None:
            default:
                return;
        }
    }

    private static void ApplyGrayscaleTintInPlace(Color[] pixels, Color tint)
    {
        var tr = tint.R / 255f;
        var tg = tint.G / 255f;
        var tb = tint.B / 255f;

        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            if (p.A == 0)
                continue;

            var lum = (0.299f * p.R + 0.587f * p.G + 0.114f * p.B) / 255f;

            pixels[i] = new Color(
                (byte)(lum * tr * 255f),
                (byte)(lum * tg * 255f),
                (byte)(lum * tb * 255f),
                p.A
            );
        }
    }

    private static void ApplyMultiplyTintInPlace(Color[] pixels, Color tint)
    {
        var tr = tint.R / 255f;
        var tg = tint.G / 255f;
        var tb = tint.B / 255f;

        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            if (p.A == 0)
                continue;

            pixels[i] = new Color(
                (byte)Math.Clamp(p.R * tr, 0f, 255f),
                (byte)Math.Clamp(p.G * tg, 0f, 255f),
                (byte)Math.Clamp(p.B * tb, 0f, 255f),
                p.A
            );
        }
    }
}
