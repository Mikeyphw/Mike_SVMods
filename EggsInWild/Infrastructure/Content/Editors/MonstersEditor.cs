using System.Collections.Generic;
using Mikew.NestCore.EnumsAndStrings;
using Mikew.NestCore.Helpers.Content;

namespace EggsInWild.Infrastructure.Content.Editors;

/// <summary>
/// Ensures custom monster keys required by DesertTomb runtime exist in Data/Monsters.
/// </summary>
public sealed class MonstersEditor : AssetEditorBase<ContentContext>
{
    public override string AssetName => Strings.AssetNames.Monsters;
    public override AssetEditPriority Priority => AssetEditPriority.Early;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, string>().Data;

        // Reuse known vanilla-compatible values for placeholders.
        data["Spinner"] =
            "30/5/0/0/false/1000/771 .9 771 .5 770 .5 382 .1 86 .005 72 .001/1/.01/3/2/.00/true/5/Spinner";
        data["The Pharaoh"] =
            "260/30/0/0/false/0/769 .75 769 .2 337 .002 336 .01 335 .02 334 .04 108 .003 -4 .1 98 .005 99 .001 74 .0005/2/.01/8/3/.0/true/15/The Pharaoh";

        foreach (var key in GetRequiredMonsterKeys())
        {
            if (data.ContainsKey(key))
                continue;

            data[key] = data["Spinner"];
        }
    }

    private static IReadOnlyList<string> GetRequiredMonsterKeys() => new[]
    {
        "Crawler",
        "Matango",
        "Hurricane",
        "Sandi",
        "The Pharaoh",
    };
}
