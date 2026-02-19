using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using StardewValley.GameData.Machines;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;
using Mikew.BirdsOfAFeather.Helpers.Content;
using Mikew.BirdsOfAFeather;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class MachinesEditor : AssetEditorBase<ContentContext>
{
    public override string AssetName => Strings.AssetNames.Machines;

        private const string DinoEggId = "107";

    private const string IncubatorOutputMethod = "StardewValley.Object, Stardew Valley: OutputIncubator";

    private static readonly object LockObj = new();
    private static IReadOnlyList<string>? _eggItemIds;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, MachineData>().Data;

        var key = $"(BC){ModItemIds.WildEggIncubator}";

        // Parity: prevent wild eggs from being used in vanilla/other incubators at all progression steps.
        EditOtherIncubators(context, data, wildEggIncubatorKey: key);

        // The Wild Egg Incubator itself is progression-gated (typically unlocked late-game).
        if (context.Step < ProgressionStep.PartV)
            return;

        data[key] = CreateIncubatorMachineData(context);
        EditWildEggIncubatorRules(context, data, wildEggIncubatorKey: key);
    }
