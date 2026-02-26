using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Game;

namespace EggsInWild.Application.Progression;

public static class ProgressionSideEffects
{
    private static readonly IReadOnlyDictionary<ProgressionStep, ProgressionStepEffects> Catalog =
        ProgressionStepEffects.BuildCatalog();

    public static void ApplyTransition(
        ProgressionStep from,
        ProgressionStep to,
        IGameMailService mail,
        IGameQuestService quests,
        IGameRecipeService recipes,
        ModConfig config)
    {
        if (!config.ProgressionMode)
            return;

        if (!config.DesertTombModuleEnabled && to == ProgressionStep.PartV)
            return;

        var ledger = ProgressionEffectsLedger.LoadOrNew();

        var desired = BuildDesired(to, config);

        // Always add missing desired effects (idempotent).
        ApplyDesired(desired, mail, quests, recipes, ledger);

        // If rolling back, reconcile by removing only what was ledger-applied and is no longer desired.
        if (to < from)
            Reconcile(desired, mail, quests, recipes, ledger);

        ledger.Save();
    }

    private static DesiredEffects BuildDesired(ProgressionStep step, ModConfig config)
    {
        var desired = new DesiredEffects();

        foreach (var s in EnumerateStepsUpTo(step))
        {
            if (!config.DesertTombModuleEnabled && s == ProgressionStep.PartV)
                continue;

            if (!Catalog.TryGetValue(s, out var effects))
                continue;

            foreach (var (mailId, noLetter) in effects.MailReceived)
                desired.MailReceived[mailId] = noLetter;

            foreach (var (mailId, noLetter) in effects.MailForTomorrow)
                desired.MailForTomorrow[mailId] = noLetter;

            desired.Quests.UnionWith(effects.Quests);
            desired.SpecialOrders.UnionWith(effects.SpecialOrders);
            desired.CraftingRecipes.UnionWith(effects.CraftingRecipes);
            desired.CookingRecipes.UnionWith(effects.CookingRecipes);
        }

        return desired;
    }

    private static void ApplyDesired(
        DesiredEffects desired,
        IGameMailService mail,
        IGameQuestService quests,
        IGameRecipeService recipes,
        ProgressionEffectsLedger ledger)
    {
        foreach (var kvp in desired.MailReceived)
        {
            mail.AddReceivedMail(kvp.Key, kvp.Value);
            ledger.MailReceived.Add(kvp.Key);
        }

        foreach (var kvp in desired.MailForTomorrow)
        {
            mail.AddMailForTomorrow(kvp.Key, kvp.Value);
            ledger.MailForTomorrow.Add(kvp.Key);
        }

        foreach (var questId in desired.Quests)
        {
            quests.AddQuest(questId);
            ledger.Quests.Add(questId);
        }

        foreach (var orderId in desired.SpecialOrders)
        {
            quests.AddSpecialOrder(orderId);
            ledger.SpecialOrders.Add(orderId);
        }

        foreach (var recipe in desired.CraftingRecipes)
        {
            recipes.EnsureCraftingRecipeLearned(recipe);
            ledger.CraftingRecipes.Add(recipe);
        }

        foreach (var recipe in desired.CookingRecipes)
        {
            recipes.EnsureCookingRecipeLearned(recipe);
            ledger.CookingRecipes.Add(recipe);
        }
    }

    private static void Reconcile(
        DesiredEffects desired,
        IGameMailService mail,
        IGameQuestService quests,
        IGameRecipeService recipes,
        ProgressionEffectsLedger ledger)
    {
        foreach (var id in ledger.MailReceived.Where(id => !desired.MailReceived.ContainsKey(id)).ToList())
        {
            if (mail.RemoveReceivedMail(id))
                ledger.MailReceived.Remove(id);
        }

        foreach (var id in ledger.MailForTomorrow.Where(id => !desired.MailForTomorrow.ContainsKey(id)).ToList())
        {
            if (mail.RemoveMailForTomorrow(id))
                ledger.MailForTomorrow.Remove(id);
        }

        foreach (var id in ledger.Quests.Where(id => !desired.Quests.Contains(id)).ToList())
        {
            if (quests.RemoveQuest(id))
                ledger.Quests.Remove(id);
        }

        foreach (var id in ledger.SpecialOrders.Where(id => !desired.SpecialOrders.Contains(id)).ToList())
        {
            if (quests.RemoveSpecialOrder(id))
                ledger.SpecialOrders.Remove(id);
        }

        foreach (var name in ledger.CraftingRecipes.Where(name => !desired.CraftingRecipes.Contains(name)).ToList())
        {
            if (recipes.ForgetCraftingRecipe(name))
                ledger.CraftingRecipes.Remove(name);
        }

        foreach (var name in ledger.CookingRecipes.Where(name => !desired.CookingRecipes.Contains(name)).ToList())
        {
            if (recipes.ForgetCookingRecipe(name))
                ledger.CookingRecipes.Remove(name);
        }
    }

    private static IEnumerable<ProgressionStep> EnumerateStepsUpTo(ProgressionStep step)
    {
        for (var s = ProgressionStep.PartI; s <= step; s++)
            yield return s;
    }

    private sealed class DesiredEffects
    {
        public Dictionary<string, bool> MailReceived { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, bool> MailForTomorrow { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Quests { get; } = new(StringComparer.Ordinal);
        public HashSet<string> SpecialOrders { get; } = new(StringComparer.Ordinal);
        public HashSet<string> CraftingRecipes { get; } = new(StringComparer.Ordinal);
        public HashSet<string> CookingRecipes { get; } = new(StringComparer.Ordinal);
    }
}
