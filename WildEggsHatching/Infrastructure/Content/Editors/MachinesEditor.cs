using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Machines;
using Mikew.BirdsOfAFeather.EnumsAndStrings;
using Mikew.BirdsOfAFeather.Helpers.Content;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;
using WildEggsHatching.Infrastructure.ModData;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class MachinesEditor : AssetEditorBase<ContentContext>, IPostPassAssetEditor<ContentContext>
{
    public override string AssetName => Strings.AssetNames.Machines;

    public override AssetEditPriority Priority => AssetEditPriority.Default;

    public IReadOnlyList<AssetPostPass<ContentContext>> PostPasses { get; } = new List<AssetPostPass<ContentContext>>
    {
        new(
            debugName: "WildEggs.Machines.EggOutputs",
            priority: AssetEditPriority.Late,
            apply: (asset, ctx) => AddMachineOutputRulesForEggs(asset, ctx)
        ),
        new(
            debugName: "WildEggs.Machines.OtherIncubators",
            priority: (AssetEditPriority)((int)AssetEditPriority.Late + 100),
            apply: (asset, ctx) => EditOtherIncubators(asset, ctx)
        ),
    };

    private const string DinoEggId = "107";

    private const string IncubatorOutputMethod =
        "StardewValley.Object, Stardew Valley: OutputIncubator";

    private const string PickledWildEggsId = "yourdorkbrains.cp_PickledWildEggs";
    private const string WildEggMayoId = "yourdorkbrains.cp_WildEggMayo";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, MachineData>().Data;

        AddMachineOutputRules(data, context);
    }

    private static void AddMachineOutputRules(
        IDictionary<string, MachineData> data,
        ContentContext context)
    {
        RemoveLegacyOutputs(data);

        var incubatorKey = $"(BC){ModItemIds.WildEggIncubator}";
        data[incubatorKey] = CreateIncubatorMachineData(context);

        ApplyIncubatorConfigOverrides(context, data[incubatorKey]);
    }

    private static void RemoveLegacyOutputs(IDictionary<string, MachineData> data)
    {
        RemoveOutputRulesByOutputItem(data, "(BC)15", PickledWildEggsId);
        RemoveOutputRulesByOutputItem(data, "(BC)24", WildEggMayoId);
    }

    private static void RemoveOutputRulesByOutputItem(
        IDictionary<string, MachineData> data,
        string machineKey,
        string outputItemId)
    {
        if (!data.TryGetValue(machineKey, out var machine) || machine?.OutputRules is null)
            return;

        machine.OutputRules.RemoveAll(rule =>
            rule?.OutputItem is not null
            && rule.OutputItem.Any(o => o is not null && string.Equals(o.ItemId, outputItemId, StringComparison.OrdinalIgnoreCase)));
    }

    private static MachineData CreateIncubatorMachineData(ContentContext context)
    {
        var eggItemIds = SpawnAreaEggIds.GetEggItemIds(context.ModData.SpawnAreas, context.Logger);

        var rules = new List<MachineOutputRule>();
        foreach (var eggId in eggItemIds.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var kind = EggKindExtensions.FromEggItemId(eggId);

            rules.Add(new MachineOutputRule
            {
                Id = eggId,
                Triggers = new List<MachineOutputTriggerRule>
                {
                    new()
                    {
                        Id = $"ItemPlacedInMachine_{eggId}",
                        Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                        RequiredItemId = eggId,
                    },
                },
                UseFirstValidOutput = true,
                OutputItem = new List<MachineItemOutput>
                {
                    new()
                    {
                        OutputMethod = IncubatorOutputMethod,
                        IncrementMachineParentSheetIndex = kind.IsLarge() ? 1 : 0,
                    },
                },
            });
        }

        return new MachineData
        {
            HasInput = false,
            HasOutput = false,
            OutputRules = rules,
            AdditionalConsumedItems = new List<MachineItemAdditionalConsumedItems>
            {
                new()
                {
                    ItemId = "259",
                    RequiredCount = 5,
                    InvalidCountMessage = "It needs 5 ferns.",
                },
            },
            ReadyTimeModifiers = new List<QuantityModifier>
            {
                new()
                {
                    Id = "Coopmaster",
                    Condition = "PLAYER_HAS_PROFESSION Target 2",
                    Modification = QuantityModifier.ModificationType.Multiply,
                    Amount = 0.5f,
                },
            },
            ReadyTimeModifierMode = QuantityModifier.QuantityModifierMode.Stack,
            InvalidItemMessage = "Only work with wild eggs.",
            InvalidItemMessageCondition = "ITEM_CATEGORY Input -5",
            LoadEffects = new List<MachineEffects>
            {
                new()
                {
                    Id = "Default",
                    Sounds = new List<MachineSoundData>
                    {
                        new() { Id = "trashcan", Delay = 0 },
                    },
                    Interval = 100,
                    ShakeDuration = -1,
                },
            },
            WorkingEffects = new List<MachineEffects>
            {
                new()
                {
                    Id = "Default",
                    Sounds = new List<MachineSoundData>
                    {
                        new() { Id = "trashcan", Delay = 0 },
                    },
                    Interval = 100,
                    ShakeDuration = -1,
                    TemporarySprites = new List<TemporaryAnimatedSpriteDefinition>
                    {
                        new()
                        {
                            Id = "Default",
                            Texture = "TileSheets\animations",
                            SourceRect = new Rectangle(0, 704, 64, 64),
                            Interval = 20f,
                            Frames = 6,
                            Loops = 999999,
                            PositionOffset = new Vector2(0, -32),
                            Scale = 0.25f,
                            AlphaFade = 0.005f,
                        },
                    },
                },
            },
            WorkingEffectChance = 0.33f,
            ShowNextIndexWhileWorking = true,
            AllowFairyDust = true,
            IsIncubator = true,
        };
    }

    private static void ApplyIncubatorConfigOverrides(ContentContext context, MachineData incubator)
    {
        if (context.Config.HatchDinoEggs)
        {
            incubator.OutputRules ??= new List<MachineOutputRule>();
            incubator.OutputRules.Add(new MachineOutputRule
            {
                Id = "DinoEgg",
                Triggers = new List<MachineOutputTriggerRule>
                {
                    new()
                    {
                        Id = "ItemPlacedInMachineDino",
                        Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                        RequiredItemId = DinoEggId,
                    },
                },
                UseFirstValidOutput = true,
                OutputItem = new List<MachineItemOutput>
                {
                    new()
                    {
                        OutputMethod = IncubatorOutputMethod,
                        IncrementMachineParentSheetIndex = 2,
                    },
                },
            });
        }

        if (!context.Config.NeedsFern)
            incubator.AdditionalConsumedItems = null;
    }

    private static void AddMachineOutputRulesForEggs(IAssetData asset, ContentContext context)
    {
        if (!TryIsRegisteredItem(PickledWildEggsId) || !TryIsRegisteredItem(WildEggMayoId))
            return;

        var data = asset.AsDictionary<string, MachineData>().Data;

        AddOutputRulesForEggKinds(data, "(BC)15", PickledWildEggsId);
        AddOutputRulesForEggKinds(data, "(BC)24", WildEggMayoId);
    }

    private static void AddOutputRulesForEggKinds(
        IDictionary<string, MachineData> data,
        string machineKey,
        string outputItemId)
    {
        if (!data.TryGetValue(machineKey, out var machine) || machine?.OutputRules is null)
            return;

        var baseTime = 200;
        var baseQty = 1;

        foreach (EggKind kind in Enum.GetValues(typeof(EggKind)))
        {
            var multiplier = kind switch
            {
                EggKind.Wild => 1,
                EggKind.LargeWild => 2,
                EggKind.Magic => 4,
                EggKind.LargeMagic => 8,
                _ => 1,
            };

            var contextTag = "wildegg_" + kind.ToSpawnParametersKey();
            var ruleId = $"{Constants.ModId}_{kind.ToSpawnParametersKey()}";

            var trigger = new MachineOutputTriggerRule
            {
                Id = contextTag,
                Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                RequiredCount = 1,
                RequiredTags = new List<string> { contextTag },
            };

            var output = new MachineItemOutput
            {
                CopyQuality = true,
                Id = outputItemId,
                ItemId = outputItemId,
                MinStack = baseQty * multiplier,
                MaxStack = baseQty * multiplier,
            };

            machine.OutputRules.Add(new MachineOutputRule
            {
                Id = ruleId,
                OutputItem = new List<MachineItemOutput> { output },
                MinutesUntilReady = baseTime * multiplier,
                Triggers = new List<MachineOutputTriggerRule> { trigger },
            });
        }
    }

    private static bool TryIsRegisteredItem(string itemId)
    {
        try
        {
            return ItemRegistry.GetData(itemId) is not null;
        }
        catch
        {
            return false;
        }
    }

    private static void EditOtherIncubators(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, MachineData>().Data;

        var wildEggIncubatorKey = $"(BC){ModItemIds.WildEggIncubator}";

        var incubators = data.Keys
            .Where(k => k is not null)
            .Where(k =>
                (k.Contains("incubator", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(k, "(BC)101", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(k, "(BC)254", StringComparison.OrdinalIgnoreCase))
                && !string.Equals(k, wildEggIncubatorKey, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in incubators)
        {
            if (!data.TryGetValue(key, out var machine) || machine is null)
                continue;

            if (machine.OutputRules is not null)
            {
                foreach (var rule in machine.OutputRules.Where(r => r is not null))
                foreach (var trig in rule.Triggers?.Where(t => t is not null) ?? Enumerable.Empty<MachineOutputTriggerRule>())
                {
                    trig.RequiredTags ??= new List<string>();

                    if (!trig.RequiredTags.Contains("!wildegg_item"))
                        trig.RequiredTags.Add("!wildegg_item");

                    if (context.Config.DinoEggsExclusiveHatch && !trig.RequiredTags.Contains("!item_dinosaur_egg"))
                        trig.RequiredTags.Add("!item_dinosaur_egg");
                }
            }

            machine.InvalidItemMessageCondition = "ITEM_CONTEXT_TAG Input wildegg_item";
            machine.InvalidItemMessage = "You can't hatch wild eggs here.";
        }
    }
}
