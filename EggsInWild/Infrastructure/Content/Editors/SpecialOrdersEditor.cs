using System.Collections.Generic;
using StardewValley.GameData.SpecialOrders;
using EggsInWild.Core.Enums;
using EggsInWild.Core.Ids;
using Mikew.NestCore.Helpers.Content;
using Mikew.NestCore.Helpers.GameStateQueries;
using Mikew.NestCore;

namespace EggsInWild.Infrastructure.Content.Editors;

public sealed class SpecialOrdersEditor : AssetEditorBase<ContentContext>
{
    public override string AssetName => Strings.AssetNames.SpecialOrders;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, SpecialOrderData>().Data;

        foreach (var (id, order) in BuildOrders(context))
            data[id] = order;
    }

    private static IReadOnlyDictionary<string, SpecialOrderData> BuildOrders(ContentContext context)
    {
        var orders = new Dictionary<string, SpecialOrderData>();

        if (context.Step >= ProgressionStep.PartI)
            orders[ProgressionIds.Quests.SpecialOrderWildEggsI] = CreateOrderI(context);

        if (context.Step >= ProgressionStep.PartIII)
            orders[ProgressionIds.Quests.SpecialOrderWildEggsII] = CreateOrderII(context);

        return orders;
    }

    private static SpecialOrderData CreateOrderI(ContentContext context)
    {
        //  condition blocks after completion and in winter.
        return new SpecialOrderData
        {
            Condition = string.Join(", ", Not(PlayerCondition.HasMail(ProgressionIds.Mail.SpecialOrderDoneI, MailType.Received, player: "Host")), Not(TimeCondition.Season("winter"))),
            Duration = QuestDuration.Month,
            Name = T(context, "specialOrder.WildEggIncubatorI.name", "Wild Eggs for Marnie"),
            Text = T(context, "specialOrder.WildEggIncubatorI.text", "Marnie asked you to gather wild eggs."),
            Requester = "Marnie",
            Repeatable = true,
            Objectives = new List<SpecialOrderObjectiveData>
            {
                new()
                {
                    Type = "Collect",
                    Data = new Dictionary<string, string> { { "AcceptedContextTags", "wildegg_item" } },
                    RequiredCount = "5",
                    Text = T(context, "specialOrder.WildEggIncubatorI.objective.0", "Collect 5 wild eggs.")
                },
                new()
                {
                    Type = "Donate",
                    Data = new Dictionary<string, string>
                    {
                        { "DropBox", "ForestWildEgg" },
                        { "DropBoxGameLocation", "Forest" },
                        { "DropBoxIndicatorLocation", "87 16" },
                        { "AcceptedContextTags", "wildegg_item" },
                    },
                    RequiredCount = "5",
                    Text = T(context, "specialOrder.WildEggIncubatorI.objective.1", "Deliver them to Marnie's drop box.")
                },
            },
            Rewards = new List<SpecialOrderRewardData>
            {
                new()
                {
                    Type = "MailReceived",
                    Data = new Dictionary<string, string> { { "MailReceived", ProgressionIds.Mail.SpecialOrderDoneI } },
                }
            },
            CustomFields = new Dictionary<string, string> { { "Untimed", "true" } },
        };
    }

    private static SpecialOrderData CreateOrderII(ContentContext context)
    {
        //  requires WizardDone flag mail.
        var useFruit = context.Config.FruitInsteadOfSeed;
        var seedOrFruitTag = useFruit ? "item_ancient_fruit" : "item_ancient_seed";
        var objective0MessageKey = useFruit
            ? "specialOrder.WildEggIncubatorII.objective.0.message_fruit"
            : "specialOrder.WildEggIncubatorII.objective.0.message_seed";
        var objective0TextKey = useFruit
            ? "specialOrder.WildEggIncubatorII.objective.0.text_fruit"
            : "specialOrder.WildEggIncubatorII.objective.0.text_seed";
        var objective0MessageFallback = useFruit ? "Bring me an ancient fruit." : "Bring me an ancient seed.";
        var objective0TextFallback = useFruit ? "Deliver an ancient fruit to the Wizard." : "Deliver an ancient seed to the Wizard.";

        return new SpecialOrderData
        {
            Condition = string.Join(", ", Not(PlayerCondition.HasMail(ProgressionIds.Mail.SpecialOrderDoneII, MailType.Received, player: "Host")), PlayerCondition.HasMail(ProgressionIds.Mail.WizardDone, MailType.Received, player: "Host")),
            Duration = QuestDuration.Month,
            Name = T(context, "specialOrder.WildEggIncubatorII.name", "Wizard's Request"),
            Text = T(context, "specialOrder.WildEggIncubatorII.text", "The Wizard needs rare ingredients."),
            Requester = "Wizard",
            Repeatable = true,
            Objectives = new List<SpecialOrderObjectiveData>
            {
                new()
                {
                    Type = "Deliver",
                    Data = new Dictionary<string, string>
                    {
                        { "Message", T(context, objective0MessageKey, objective0MessageFallback) },
                        { "TargetName", "Wizard" },
                        { "AcceptedContextTags", seedOrFruitTag },
                    },
                    RequiredCount = "1",
                    Text = T(context, objective0TextKey, objective0TextFallback)
                },
                new()
                {
                    Type = "Deliver",
                    Data = new Dictionary<string, string>
                    {
                        { "Message", T(context, "specialOrder.WildEggIncubatorII.objective.2.message", "A dinosaur egg would be most useful.") },
                        { "TargetName", "Wizard" },
                        { "AcceptedContextTags", "item_dinosaur_egg" },
                    },
                    RequiredCount = "1",
                    Text = T(context, "specialOrder.WildEggIncubatorII.objective.2.text", "Deliver a dinosaur egg to the Wizard.")
                },
                new()
                {
                    Type = "Deliver",
                    Data = new Dictionary<string, string>
                    {
                        { "Message", T(context, "specialOrder.WildEggIncubatorII.objective.1.message", "A jar of ancient fruit jelly may do.") },
                        { "TargetName", "Wizard" },
                        { "AcceptedContextTags", "preserve_sheet_index_734" },
                    },
                    RequiredCount = "1",
                    Text = T(context, "specialOrder.WildEggIncubatorII.objective.1.text", "Deliver ancient fruit jelly to the Wizard.")
                },
            },
            Rewards = new List<SpecialOrderRewardData>
            {
                new()
                {
                    Type = "MailReceived",
                    Data = new Dictionary<string, string> { { "MailReceived", ProgressionIds.Mail.SpecialOrderDoneII } },
                }
            },
            CustomFields = new Dictionary<string, string> { { "Untimed", "true" } },
        };
    }

    private static string Not(string query)
        => "!" + query;

    private static string T(ContentContext context, string key, string fallback)
    {
        var translated = context.I18n.Get(key).ToString();
        return string.Equals(translated, key, System.StringComparison.OrdinalIgnoreCase) ? fallback : translated;
    }
}
