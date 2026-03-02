namespace Mikew.NestCore.Integrations.StardewUI;

/// <summary>Convention-based paths and prefixes for StardewUI asset registration.</summary>
/// <param name="ViewsDir">Physical directory under the mod folder containing <c>.sml</c> view files.</param>
/// <param name="SpritesDir">Physical directory under the mod folder containing sprite assets.</param>
/// <param name="CustomDataDir">
/// Optional physical directory under the mod folder containing StardewUI custom data assets.
/// </param>
/// <param name="ViewsPrefix">Optional asset-name prefix; defaults to <c>Mods/{UniqueID}/Views</c>.</param>
/// <param name="SpritesPrefix">Optional asset-name prefix; defaults to <c>Mods/{UniqueID}/Sprites</c>.</param>
/// <param name="CustomDataPrefix">Optional asset-name prefix; defaults to <c>Mods/{UniqueID}</c>.</param>
public sealed record StardewUiAssetsOptions(
    string ViewsDir = "assets/views",
    string SpritesDir = "assets/sprites",
    string? CustomDataDir = null,
    string? ViewsPrefix = null,
    string? SpritesPrefix = null,
    string? CustomDataPrefix = null);
