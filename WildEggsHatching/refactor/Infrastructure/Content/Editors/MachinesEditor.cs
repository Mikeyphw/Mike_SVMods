using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using StardewValley.GameData.Machines;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class MachinesEditor : AssetEditorBase
{
    public override string AssetName => GameAssets.Machines;

    private const string IncubatorOutputMethod = "StardewValley.Object, Stardew Valley: OutputIncubator";

    private static readonly object LockObj = new();
    private static IReadOnlyList<string>? _eggItemIds;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        // The incubator is progression-gated (typically unlocked late-game).
        if (context.Step < ProgressionStep.PartV)
            return;

        var data = asset.AsDictionary<string, MachineData>().Data;

        var key = $"(BC){ModItemIds.WildEggIncubator}";
        data[key] = CreateIncubatorMachineData(context);
    }

    private static MachineData CreateIncubatorMachineData(ContentContext context)
        => new()
        {
            HasInput = false,
            HasOutput = false,
            InteractMethod = null,
            OutputRules = CreateOutputRules(context),
            AdditionalConsumedItems = CreateAdditionalConsumedItems(),
            ReadyTimeModifiers = CreateReadyTimeModifiers(),
            ReadyTimeModifierMode = QuantityModifier.QuantityModifierMode.Stack,
            InvalidItemMessage = "Only works with wild eggs.",
            InvalidItemMessageCondition = "ITEM_CATEGORY Input -5",
            LoadEffects = new List<MachineEffects>(),
            WorkingEffects = new List<MachineEffects>(),
            WorkingEffectChance = 0.33f,
            WobbleWhileWorking = false,
            LightWhileWorking = null,
            ShowNextIndexWhileWorking = true,
            AllowFairyDust = true,
            IsIncubator = true,
        };

    private static List<MachineOutputRule> CreateOutputRules(ContentContext context)
    {
        var rules = new List<MachineOutputRule>();

        foreach (var eggId in GetEggItemIds(context))
        {
            var kind = EggKindExtensions.FromEggItemId(eggId);

            if (kind.IsMagic() && context.Step < ProgressionStep.PartVI)
                continue;

            rules.Add(CreateOutputRule(eggId, incrementIndex: kind.IsLarge()));
        }

        return rules;
    }

    private static MachineOutputRule CreateOutputRule(string requiredItemId, bool incrementIndex)
        => new()
        {
            Id = requiredItemId,
            Triggers = new List<MachineOutputTriggerRule>
            {
                new()
                {
                    Id = $"ItemPlacedInMachine_{requiredItemId}",
                    Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                    RequiredItemId = requiredItemId,
                },
            },
            UseFirstValidOutput = true,
            OutputItem = new List<MachineItemOutput>
            {
                new()
                {
                    OutputMethod = IncubatorOutputMethod,
                    IncrementMachineParentSheetIndex = incrementIndex ? 1 : 0,
                },
            },
        };

    private static List<MachineItemAdditionalConsumedItems> CreateAdditionalConsumedItems()
        => new()
        {
            new()
            {
                ItemId = "259",
                RequiredCount = 5,
                InvalidCountMessage = "It needs 5 ferns.",
            },
        };

    private static List<QuantityModifier> CreateReadyTimeModifiers()
        => new()
        {
            new()
            {
                Id = "Coopmaster",
                Condition = "PLAYER_HAS_PROFESSION Target 2",
                Modification = QuantityModifier.ModificationType.Multiply,
                Amount = 0.5f,
            },
        };

    private static IReadOnlyList<string> GetEggItemIds(ContentContext context)
    {
        if (_eggItemIds is not null)
            return _eggItemIds;

        lock (LockObj)
        {
            if (_eggItemIds is not null)
                return _eggItemIds;

            var path = Path.Combine(context.Helper.DirectoryPath, "ModData", "SpawnAreas.json");
            if (!File.Exists(path))
            {
                context.Log($"MachinesEditor: SpawnAreas.json not found at '{path}'.", StardewModdingAPI.LogLevel.Warn);
                _eggItemIds = Array.Empty<string>();
                return _eggItemIds;
            }

            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream);

            var ids = new List<string>();
            foreach (var prop in doc.RootElement.EnumerateObject())
                ids.Add(prop.Name);

            _eggItemIds = ids;
            return _eggItemIds;
        }
    }
}
