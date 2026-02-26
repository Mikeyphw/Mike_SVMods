using System;
using System.Collections.Generic;
using System.Text.Json;
using StardewValley;

namespace EggsInWild.Application.Progression;

/// <summary>
/// Tracks which progression side effects were applied by console forcing so that rollbacks can safely undo only mod-granted state.
/// </summary>
public sealed class ProgressionEffectsLedger
{
    private const string LedgerKey = "EggsInWild/ProgressionEffectsLedger";

    public HashSet<string> MailReceived { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> MailForTomorrow { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> ActiveQuests { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> CompletedQuests { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> CompletedSpecialOrders { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> CraftingRecipes { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> CookingRecipes { get; set; } = new(StringComparer.Ordinal);

    public static ProgressionEffectsLedger LoadOrNew()
    {
        try
        {
            if (Game1.player?.modData is null)
                return new ProgressionEffectsLedger();

            if (!Game1.player.modData.TryGetValue(LedgerKey, out var json) || string.IsNullOrWhiteSpace(json))
                return new ProgressionEffectsLedger();

            return JsonSerializer.Deserialize<ProgressionEffectsLedger>(json) ?? new ProgressionEffectsLedger();
        }
        catch
        {
            return new ProgressionEffectsLedger();
        }
    }

    public void Save()
    {
        try
        {
            if (Game1.player?.modData is null)
                return;

            Game1.player.modData[LedgerKey] = JsonSerializer.Serialize(this);
        }
        catch
        {
            // ignored
        }
    }
}
