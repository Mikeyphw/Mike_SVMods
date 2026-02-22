using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Mikew.BirdsOfAFeather.EnumsAndStrings;
using Mikew.BirdsOfAFeather.Helpers.Content;
using StardewValley.GameData.Monsters;

namespace WildEggsHatching.Infrastructure.Content.Editors;

/// <summary>
/// Ensures custom <see cref="StardewValley.Monsters.Monster"/> subclasses have placeholder entries in Data/Monsters.
/// </summary>
public sealed class MonstersEditor : AssetEditorBase<ContentContext>
{
    public override string AssetName => Strings.AssetNames.Monsters;

    public override AssetEditPriority Priority => AssetEditPriority.Early;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, MonsterData>().Data;

        var template = ResolveTemplate(data);
        if (template is null)
            return;

        foreach (var key in GetRequiredMonsterKeys())
        {
            if (data.ContainsKey(key))
                continue;

            data[key] = Clone(template);
        }
    }

    private static MonsterData? ResolveTemplate(IDictionary<string, MonsterData> data)
    {
        if (data.TryGetValue("Mummy", out var mummy))
            return mummy;

        if (data.TryGetValue("Spider", out var spider))
            return spider;

        if (data.Count > 0)
            return data.Values.FirstOrDefault();

        return null;
    }

    private static IReadOnlyList<string> GetRequiredMonsterKeys() => new[]
    {
        // Custom Monster subclasses (direct or indirect). Placeholder entries are valid and may be replaced later.
        "Crawler",
        "Matango",
        "Hurricane",
        "Sandi",
        "The Pharaoh",
    };

    private static MonsterData Clone(MonsterData template)
    {
        // MonsterData is a complex model; use JSON round-trip to avoid manual copy and future schema drift.
        var json = JsonSerializer.Serialize(template);
        return JsonSerializer.Deserialize<MonsterData>(json) ?? new MonsterData();
    }
}
