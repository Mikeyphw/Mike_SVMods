using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LetMeHatch.Core.ClonePipeline;
using LetMeHatch.Infrastructure.ClonePipeline;
using LetMeHatch.Core.Cloning;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using Mikew.NestCore.Helpers.Content;
using LetMeHatch.Infrastructure.Content;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Infrastructure.Content.Loaders;

/// <summary>
/// Dynamically generates per-clone 16x16 textures under <c>Mods/&lt;UniqueId&gt;/Objects/Clones/&lt;cloneId&gt;</c>.
/// </summary>
public sealed class DynamicCloneObjectTextureLoader : IAssetRequestHandler<ContentContext>, IContentHandlerDebugInfo
{
    private readonly string _prefixLower;
    private readonly int _prefixLen;

    private static readonly ConcurrentDictionary<string, Texture2D> Cache = new(StringComparer.Ordinal);

    public DynamicCloneObjectTextureLoader(string modUniqueId)
    {
        if (string.IsNullOrWhiteSpace(modUniqueId))
            throw new ArgumentException("Mod unique id must be non-empty.", nameof(modUniqueId));

        _prefixLower = $"mods/{modUniqueId.ToLowerInvariant()}/objects/clones/";
        _prefixLen = _prefixLower.Length;
    }

    
    public int Order => (int)AssetLoadPriority.Exclusive;

    public bool StopAfterHandle => false;

    public bool IsMatch(AssetRequestedEventArgs e)
    {
        var nameLower = e.NameWithoutLocale.BaseName.ToLowerInvariant();
        return CanLoad(nameLower, e);
    }

    public bool ShouldApply(ContentContext context, AssetRequestedEventArgs e) => true;

    public void Handle(AssetRequestedEventArgs e, ContentContext context)
    {
        var nameLower = e.NameWithoutLocale.BaseName.ToLowerInvariant();
        Load(nameLower, e);
    }

    public string DebugHandles => "Mods/<UniqueId>/Objects/Clones/<cloneId>";

    public string DebugGates => "assetNameLower starts with mods/<uniqueid>/objects/clones/.";

    private bool CanLoad(string assetNameLower, AssetRequestedEventArgs e)
        => assetNameLower.StartsWith(_prefixLower, StringComparison.Ordinal);

    private void Load(string assetNameLower, AssetRequestedEventArgs e)
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

        var keys = new List<string>(capacity: Math.Min(maxKeys, Cache.Count));
        foreach (var key in Cache.Keys)
        {
            keys.Add(key);
            if (keys.Count >= maxKeys)
                break;
        }

        return keys.ToArray();
    }

    private static Texture2D BuildTexture(string cloneId)
    {
        Game1.objectData.TryGetValue(cloneId, out var cloneData);

        if (!CloneIdService.TryParseCloneId(cloneId, out var animalKey, out var originalItemId, out _))
        {
            LogWarn($"Not a recognized clone id: {cloneId}. Using transparent fallback texture.");
            return CreateTransparentTexture();
        }

        if (!Game1.objectData.TryGetValue(originalItemId, out var originalData))
        {
            LogWarn($"Original object not found: {originalItemId} (from clone {cloneId}). Using transparent fallback texture.");
            return CreateTransparentTexture();
        }

        var sourceTexture = LoadOriginalTextureWithFallback(originalData.Texture, cloneId, originalItemId);
        var pixels = CropSprite(sourceTexture, originalData.SpriteIndex, cloneId, originalItemId);

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

    private static Texture2D LoadOriginalTextureWithFallback(string? textureName, string cloneId, string originalItemId)
    {
        foreach (var candidate in GetTextureLoadCandidates(textureName))
        {
            try
            {
                var texture = Game1.content.Load<Texture2D>(candidate);
                if (texture is not null)
                    return texture;
            }
            catch (Exception ex)
            {
                LogTrace($"Failed loading texture candidate '{candidate}' for clone '{cloneId}' (original {originalItemId}): {ex.Message}");
            }
        }

        LogWarn(
            $"Failed to load texture '{textureName ?? "<null>"}' for clone '{cloneId}' (original {originalItemId}). " +
            "Falling back to transparent texture."
        );
        return CreateTransparentTexture();
    }

    private static IEnumerable<string> GetTextureLoadCandidates(string? textureName)
    {
        if (!string.IsNullOrWhiteSpace(textureName))
        {
            yield return textureName;

            var slashIndex = textureName.IndexOf('/');
            if (slashIndex > 0 && !char.IsUpper(textureName[0]))
            {
                var normalized = char.ToUpperInvariant(textureName[0]) + textureName[1..];
                if (!string.Equals(normalized, textureName, StringComparison.Ordinal))
                    yield return normalized;
            }
        }

        const string defaultTexture = "Maps/springobjects";
        yield return defaultTexture;
        yield return "Maps/SpringObjects";
    }

    private static Texture2D ToTexture(Color[] pixels)
    {
        var tex = new Texture2D(Game1.graphics.GraphicsDevice, 16, 16);
        tex.SetData(pixels);
        return tex;
    }

    private static Texture2D CreateTransparentTexture()
    {
        var pixels = new Color[16 * 16];
        return ToTexture(pixels);
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

    private static Color[] CropSprite(Texture2D texture, int spriteIndex, string cloneId, string originalItemId)
    {
        const int tileSize = 16;
        var columns = Math.Max(1, texture.Width / tileSize);
        var rows = Math.Max(1, texture.Height / tileSize);
        var maxSprites = columns * rows;
        if (maxSprites <= 0)
            return new Color[tileSize * tileSize];

        var clampedSpriteIndex = Math.Clamp(spriteIndex, 0, maxSprites - 1);
        if (clampedSpriteIndex != spriteIndex)
        {
            LogTrace(
                $"Sprite index {spriteIndex} out of range for clone '{cloneId}' (original {originalItemId}) " +
                $"on texture {texture.Width}x{texture.Height}; clamped to {clampedSpriteIndex}."
            );
        }

        var x = (clampedSpriteIndex % columns) * tileSize;
        var y = (clampedSpriteIndex / columns) * tileSize;

        var data = new Color[tileSize * tileSize];
        texture.GetData(0, new Rectangle(x, y, tileSize, tileSize), data, 0, data.Length);
        return data;
    }

    private static void LogWarn(string message)
    {
        ModEntry.MonitorInstance.Log($"[DynamicCloneObjectTextureLoader] {message}", LogLevel.Warn);
    }

    private static void LogTrace(string message)
    {
        ModEntry.MonitorInstance.Log($"[DynamicCloneObjectTextureLoader] {message}", LogLevel.Trace);
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
