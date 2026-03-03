// FILE: NestCore/Integrations/StardewUI/StardewUiAssetsOptions.cs
namespace Mikew.NestCore.Integrations.StardewUI;

/// <summary>Convention-based paths/prefixes for StardewUI asset registration.</summary>
public sealed record StardewUiAssetsOptions(
    string ViewsDir = "assets/views",
    string SpritesDir = "assets/sprites",
    string? CustomDataDir = null,
    string? ViewsPrefix = null,
    string? SpritesPrefix = null,
    string? CustomDataPrefix = null);
