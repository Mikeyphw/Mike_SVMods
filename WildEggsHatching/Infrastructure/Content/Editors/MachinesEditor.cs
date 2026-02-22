using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using StardewValley.GameData.Machines;
using Mikew.BirdsOfAFeather;
using Mikew.BirdsOfAFeather.Helpers.Content;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;
using WildEggsHatching.Infrastructure.ModData;

namespace WildEggsHatching.Infrastructure.Content.Editors;

/// <summary>
/// Edits <c>Data/Machines</c> for incubator behavior and egg product machines.
/// Uses a BOAF post-pass to apply compatibility fixes after most edits.
/// </summary>
public sealed class MachinesEditor : AssetEditorBase<ContentContext>, IPostPassAssetEditor<ContentContext>
{
    public override string AssetName => Strings.AssetNames.Machines;

    /// <summary>
    /// Run compatibility repairs at a very-late priority (original parity: Late+100).
    /// </summary>
    public AssetEditPriority PostPriority => unchecked((AssetEditPriority)9999);

    private const string DinoEggId = "107";
    private const string IncubatorOutputMethod = "StardewValley.Object, Stardew Valley: OutputIncubator";

    private const string PreserveJarKey = "(BC)15";
    private const string MayonnaiseMachineKey = "(BC)24";

    private const string CpPickledWildEggs = "yourdorkbrains.cp_PickledWildEggs";
    private const string CpWildEggMayo = "yourdorkbrains.cp_WildEggMayo";

    private static readonly object LockObj = new();
    private static IReadOnlyList<string>? _eggItemIdsCache;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, MachineData>().Data;

        ApplyEggProductRules(context, data);

        var incubatorKey = $"(BC){ModItemIds.WildEggIncubator}";
        data[incubatorKey] = CreateWildEggIncubatorMachineData(context);

        EditWildEggIncubatorRules(context, data, wildEggIncubatorKey: incubatorKey);
    }

    public void EditPost(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, MachineData>().Data;
        var incubatorKey = $"(BC){ModItemIds.WildEggIncubator}";

        // Parity: prevent wild eggs from being used in vanilla/other incubators at all progression steps.
        EditOtherIncubators(context, data, wildEggIncubatorKey: incubatorKey);
    }

    private static IReadOnlyList<string> GetEggItemIds(ContentContext context)
    {
        lock (LockObj)
        {
            _eggItemIdsCache ??= SpawnAreaEggIds.GetEggItemIds(context.ModData.SpawnAreas, context.Logger);
            return _eggItemIdsCache;
        }
    }

    private static MachineData CreateWildEggIncubatorMachineData(ContentContext context)
    {
        return new MachineData
        {
            HasInput = false,
            HasOutput = false,
            InteractMethod = null,
            OutputRules = CreateIncubatorOutputRules(context),
            AdditionalConsumedItems = context.Config.NeedsFern ? CreateFernCosts() : null,
            ReadyTimeModifiers = CreateReadyTimeModifiers(),
            ReadyTimeModifierMode = QuantityModifier.QuantityModifierMode.Stack,
            InvalidItemMessage = "Only works with wild eggs.",
            InvalidItemMessageCondition = "ITEM_CATEGORY Input -5",
            InvalidCountMessage = null,
            LoadEffects = CreateLoadEffects(),
            WorkingEffects = CreateWorkingEffects(),
            WorkingEffectChance = 0.33f,
            WobbleWhileWorking = false,
            LightWhileWorking = null,
            ShowNextIndexWhileWorking = true,
            AllowFairyDust = true,
            IsIncubator = true,
        };
    }

    private static List<MachineOutputRule> CreateIncubatorOutputRules(ContentContext context)
    {
        var rules = new List<MachineOutputRule>();

        foreach (var eggId in GetEggItemIds(context))
        {
            rules.Add(CreateIncubatorOutputRule(eggId, incrementIndex: IsLargeEggId(eggId) ? 1 : 0));
        }

        return rules;
    }

    private static MachineOutputRule CreateIncubatorOutputRule(string requiredItemId, int incrementIndex)
    {
        return new MachineOutputRule
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
                    IncrementMachineParentSheetIndex = incrementIndex,
                },
            },
        };
    }

    private static List<MachineItemAdditionalConsumedItems> CreateFernCosts()
    {
        return new List<MachineItemAdditionalConsumedItems>
        {
            new()
            {
                ItemId = "259",
                RequiredCount = 5,
                InvalidCountMessage = "It needs 5 ferns.",
            },
        };
    }

    private static List<QuantityModifier> CreateReadyTimeModifiers()
    {
        return new List<QuantityModifier>
        {
            new()
            {
                Id = "Coopmaster",
                Condition = "PLAYER_HAS_PROFESSION Target 2",
                Modification = QuantityModifier.ModificationType.Multiply,
                Amount = 0.5f,
            },
        };
    }

    private static List<MachineEffects> CreateLoadEffects()
    {
        return new List<MachineEffects>
        {
            new()
            {
                Id = "Default",
                Condition = null,
                Sounds = new List<MachineSoundData> { new() { Id = "trashcan", Delay = 0 } },
                Interval = 100,
                ShakeDuration = -1,
            },
        };
    }

    private static List<MachineEffects> CreateWorkingEffects()
    {
        return new List<MachineEffects>
        {
            new()
            {
                Id = "Default",
                Condition = null,
                Sounds = new List<MachineSoundData> { new() { Id = "trashcan", Delay = 0 } },
                Interval = 100,
                ShakeDuration = -1,
                TemporarySprites = new List<TemporaryAnimatedSpriteDefinition>
                {
                    new()
                    {
                        Id = "Default",
                        Texture = "TileSheets\\animations",
                        SourceRect = new Rectangle(0, 704, 64, 64),
                        Interval = 20f,
                        Frames = 6,
                        Loops = 999999,
                        PositionOffset = new Vector2(0, -32),
                        Flicker = false,
                        Scale = 0.25f,
                        AlphaFade = 0.005f,
                    },
                },
            },
        };
    }

    private static void EditWildEggIncubatorRules(ContentContext context, IDictionary<string, MachineData> data, string wildEggIncubatorKey)
    {
        if (!data.TryGetValue(wildEggIncubatorKey, out var incubator))
            return;

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
        {
            incubator.AdditionalConsumedItems = null;
        }
    }

    private static void ApplyEggProductRules(ContentContext context, IDictionary<string, MachineData> data)
    {
        if (context.Step < ProgressionStep.PartVI)
            return;

        if (data.TryGetValue(PreserveJarKey, out var preserveJar))
            RemoveOutputRulesByItemId(preserveJar, CpPickledWildEggs);

        if (data.TryGetValue(MayonnaiseMachineKey, out var mayo))
            RemoveOutputRulesByItemId(mayo, CpWildEggMayo);

        if (data.TryGetValue(PreserveJarKey, out preserveJar))
            AddEggTypeRules(preserveJar, CpPickledWildEggs);

        if (data.TryGetValue(MayonnaiseMachineKey, out mayo))
            AddEggTypeRules(mayo, CpWildEggMayo);
    }

    private static void RemoveOutputRulesByItemId(MachineData machine, string itemId)
    {
        if (machine.OutputRules is null || machine.OutputRules.Count == 0)
            return;

        machine.OutputRules.RemoveAll(rule =>
            rule.OutputItem != null
            && rule.OutputItem.Any(o => string.Equals(o.ItemId, itemId, StringComparison.OrdinalIgnoreCase)));
    }

    private static void AddEggTypeRules(MachineData machine, string outputItemId)
    {
        machine.OutputRules ??= new List<MachineOutputRule>();

        const int baseTime = 200;
        const int baseQuantity = 1;

        foreach (var eggType in new[]
                 {
                     EggType.wildEgg,
                     EggType.largeWildEgg,
                     EggType.magicWildEgg,
                     EggType.largeMagicWildEgg,
                 })
        {
            var quantity = eggType switch
            {
                EggType.wildEgg => baseQuantity,
                EggType.largeWildEgg => baseQuantity * 2,
                EggType.magicWildEgg => baseQuantity * 4,
                EggType.largeMagicWildEgg => baseQuantity * 8,
                _ => baseQuantity,
            };

            var time = eggType switch
            {
                EggType.wildEgg => baseTime,
                EggType.largeWildEgg => baseTime * 2,
                EggType.magicWildEgg => baseTime * 4,
                EggType.largeMagicWildEgg => baseTime * 8,
                _ => baseTime,
            };

            var tag = "wildegg_" + eggType;

            machine.OutputRules.Add(new MachineOutputRule
            {
                Id = $"{Constants.ModId}_{eggType}",
                OutputItem = new List<MachineItemOutput>
                {
                    new()
                    {
                        CopyQuality = true,
                        Id = outputItemId,
                        ItemId = outputItemId,
                        MinStack = quantity,
                        MaxStack = quantity,
                    },
                },
                MinutesUntilReady = time,
                Triggers = new List<MachineOutputTriggerRule>
                {
                    new()
                    {
                        Id = tag,
                        Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                        RequiredCount = 1,
                        RequiredTags = new List<string> { tag },
                    },
                },
            });
        }
    }

    private static void EditOtherIncubators(ContentContext context, IDictionary<string, MachineData> data, string wildEggIncubatorKey)
    {
        var keys = data.Keys.ToList();

        foreach (var key in keys)
        {
            if (string.Equals(key, wildEggIncubatorKey, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!IsIncubatorKey(key))
                continue;

            var machine = data[key];
            if (machine.OutputRules is null)
                continue;

            foreach (var outputRule in machine.OutputRules)
            {
                if (outputRule.Triggers is null)
                    continue;

                foreach (var trigger in outputRule.Triggers)
                {
                    trigger.RequiredTags ??= new List<string>();
                    trigger.RequiredTags.Add("!wildegg_item");

                    if (context.Config.DinoEggsExclusiveHatch)
                        trigger.RequiredTags.Add("!item_dinosaur_egg");
                }
            }

            machine.InvalidItemMessageCondition = "ITEM_CONTEXT_TAG Input wildegg_item";
            machine.InvalidItemMessage = "You can't hatch wild eggs here.";
        }
    }

    private static bool IsIncubatorKey(string key)
    {
        if (key.IndexOf("incubator", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (string.Equals(key, $"(BC){BaseGameBigCraftableIds.Incubator}", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(key, $"(BC){BaseGameBigCraftableIds.OstrichIncubator}", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool IsLargeEggId(string eggItemId)
    {
        return eggItemId.EndsWith("LWEgg", StringComparison.OrdinalIgnoreCase)
            || eggItemId.EndsWith("LMWEgg", StringComparison.OrdinalIgnoreCase)
            || eggItemId.EndsWith("largeWildEgg", StringComparison.OrdinalIgnoreCase)
            || eggItemId.EndsWith("largeMagicWildEgg", StringComparison.OrdinalIgnoreCase);
    }
}
