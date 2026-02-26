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
        ProgressionSaveData saveData,
        int dayNumber,
        ModConfig config)
    {
        if (!config.ProgressionMode)
            return;

        if (!config.DesertTombModuleEnabled && to == ProgressionStep.PartV)
            return;

        var ledger = ProgressionEffectsLedger.LoadOrNew();
        var desired = BuildDesired(to, config);

        ApplyDesired(desired, mail, quests, recipes, ledger);
        ApplyCompletionDays(desired, saveData, dayNumber, config);

        if (to < from)
        {
            Reconcile(desired, mail, quests, recipes, ledger);
            ReconcileCompletionDays(desired, saveData, ledger);
        }

        ledger.Save();
    }

    public static string PreviewTransition(
        ProgressionStep from,
        ProgressionStep to,
        IGameMailService mail,
        IGameQuestService quests,
        IGameRecipeService recipes,
        ModConfig config)
    {
        var ledger = ProgressionEffectsLedger.LoadOrNew();
        var desired = BuildDesired(to, config);

        var addMailReceived = desired.MailReceived.Keys.Where(id => !mail.HasReceivedMail(id)).OrderBy(s => s, StringComparer.Ordinal).ToList();
        var addMailTomorrow = desired.MailForTomorrow.Keys.Where(id => !mail.HasMailForTomorrow(id) && !mail.HasReceivedMail(id)).OrderBy(s => s, StringComparer.Ordinal).ToList();

        var addActiveQuests = desired.ActiveQuests.Where(id => !quests.HasQuestInLog(id) && !quests.HasCompletedQuest(id)).OrderBy(s => s, StringComparer.Ordinal).ToList();
        var addCompletedQuests = desired.CompletedQuests.Where(id => !quests.HasCompletedQuest(id)).OrderBy(s => s, StringComparer.Ordinal).ToList();
        var addCompletedOrders = desired.CompletedSpecialOrders.Where(id => !quests.HasCompletedSpecialOrder(id)).OrderBy(s => s, StringComparer.Ordinal).ToList();

        var addCrafting = desired.CraftingRecipes.Where(name => !recipes.HasCraftingRecipe(name)).OrderBy(s => s, StringComparer.Ordinal).ToList();
        var addCooking = desired.CookingRecipes.Where(name => !recipes.HasCookingRecipe(name)).OrderBy(s => s, StringComparer.Ordinal).ToList();

        var removeMailReceived = new List<string>();
        var removeMailTomorrow = new List<string>();
        var removeActiveQuests = new List<string>();
        var removeCompletedQuests = new List<string>();
        var removeCompletedOrders = new List<string>();
        var removeCrafting = new List<string>();
        var removeCooking = new List<string>();

        if (to < from)
        {
            removeMailReceived = ledger.MailReceived.Where(id => !desired.MailReceived.ContainsKey(id)).OrderBy(s => s, StringComparer.Ordinal).ToList();
            removeMailTomorrow = ledger.MailForTomorrow.Where(id => !desired.MailForTomorrow.ContainsKey(id)).OrderBy(s => s, StringComparer.Ordinal).ToList();
            removeActiveQuests = ledger.ActiveQuests.Where(id => !desired.ActiveQuests.Contains(id)).OrderBy(s => s, StringComparer.Ordinal).ToList();
            removeCompletedQuests = ledger.CompletedQuests.Where(id => !desired.CompletedQuests.Contains(id)).OrderBy(s => s, StringComparer.Ordinal).ToList();
            removeCompletedOrders = ledger.CompletedSpecialOrders.Where(id => !desired.CompletedSpecialOrders.Contains(id)).OrderBy(s => s, StringComparer.Ordinal).ToList();
            removeCrafting = ledger.CraftingRecipes.Where(name => !desired.CraftingRecipes.Contains(name)).OrderBy(s => s, StringComparer.Ordinal).ToList();
            removeCooking = ledger.CookingRecipes.Where(name => !desired.CookingRecipes.Contains(name)).OrderBy(s => s, StringComparer.Ordinal).ToList();
        }

        return FormatPreview(from, to,
            addMailReceived, addMailTomorrow,
            addActiveQuests, addCompletedQuests, addCompletedOrders,
            addCrafting, addCooking,
            removeMailReceived, removeMailTomorrow,
            removeActiveQuests, removeCompletedQuests, removeCompletedOrders,
            removeCrafting, removeCooking);
    }

    private static string FormatPreview(
        ProgressionStep from,
        ProgressionStep to,
        IReadOnlyList<string> addMailReceived,
        IReadOnlyList<string> addMailTomorrow,
        IReadOnlyList<string> addActiveQuests,
        IReadOnlyList<string> addCompletedQuests,
        IReadOnlyList<string> addCompletedOrders,
        IReadOnlyList<string> addCrafting,
        IReadOnlyList<string> addCooking,
        IReadOnlyList<string> removeMailReceived,
        IReadOnlyList<string> removeMailTomorrow,
        IReadOnlyList<string> removeActiveQuests,
        IReadOnlyList<string> removeCompletedQuests,
        IReadOnlyList<string> removeCompletedOrders,
        IReadOnlyList<string> removeCrafting,
        IReadOnlyList<string> removeCooking)
    {
        var lines = new List<string>
        {
            $"Step effects preview: {from} -> {to}",
        };

        void Section(string title, IReadOnlyList<string> items)
        {
            if (items.Count == 0)
                return;

            lines.Add("");
            lines.Add(title);
            foreach (var s in items)
                lines.Add($"  - {s}");
        }

        Section("Add: MailReceived", addMailReceived);
        Section("Add: MailForTomorrow", addMailTomorrow);
        Section("Add: Quests (active)", addActiveQuests);
        Section("Add: Quests (complete)", addCompletedQuests);
        Section("Add: SpecialOrders (complete)", addCompletedOrders);
        Section("Add: CraftingRecipes", addCrafting);
        Section("Add: CookingRecipes", addCooking);

        Section("Remove (rollback-safe): MailReceived", removeMailReceived);
        Section("Remove (rollback-safe): MailForTomorrow", removeMailTomorrow);
        Section("Remove (rollback-safe): Quests (active)", removeActiveQuests);
        Section("Remove (rollback-safe): Quests (complete)", removeCompletedQuests);
        Section("Remove (rollback-safe): SpecialOrders (complete)", removeCompletedOrders);
        Section("Remove (rollback-safe): CraftingRecipes", removeCrafting);
        Section("Remove (rollback-safe): CookingRecipes", removeCooking);

        if (lines.Count == 1)
            lines.Add("\n(no changes would be applied)");

        return string.Join("\n", lines);
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

            desired.ActiveQuests.UnionWith(effects.ActiveQuests);
            desired.CompletedQuests.UnionWith(effects.CompletedQuests);
            desired.CompletedSpecialOrders.UnionWith(effects.CompletedSpecialOrders);
            desired.CraftingRecipes.UnionWith(effects.CraftingRecipes);
            desired.CookingRecipes.UnionWith(effects.CookingRecipes);
        }

        // If a quest is completed, it doesn't need to remain active.
        desired.ActiveQuests.ExceptWith(desired.CompletedQuests);

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
            if (mail.HasReceivedMail(kvp.Key))
                continue;

            mail.AddReceivedMail(kvp.Key, kvp.Value);
            ledger.MailReceived.Add(kvp.Key);
        }

        foreach (var kvp in desired.MailForTomorrow)
        {
            if (mail.HasMailForTomorrow(kvp.Key) || mail.HasReceivedMail(kvp.Key))
                continue;

            mail.AddMailForTomorrow(kvp.Key, kvp.Value);
            ledger.MailForTomorrow.Add(kvp.Key);
        }

        foreach (var questId in desired.ActiveQuests)
        {
            if (quests.HasCompletedQuest(questId) || quests.HasQuestInLog(questId))
                continue;

            quests.AddQuest(questId);
            ledger.ActiveQuests.Add(questId);
        }

        foreach (var questId in desired.CompletedQuests)
        {
            if (quests.HasCompletedQuest(questId))
                continue;

            if (quests.CompleteQuest(questId))
                ledger.CompletedQuests.Add(questId);
        }

        foreach (var orderId in desired.CompletedSpecialOrders)
        {
            if (quests.HasCompletedSpecialOrder(orderId))
                continue;

            if (quests.MarkSpecialOrderCompleted(orderId))
                ledger.CompletedSpecialOrders.Add(orderId);
        }

        foreach (var recipe in desired.CraftingRecipes)
        {
            if (recipes.HasCraftingRecipe(recipe))
                continue;

            if (recipes.EnsureCraftingRecipeLearned(recipe))
                ledger.CraftingRecipes.Add(recipe);
        }

        foreach (var recipe in desired.CookingRecipes)
        {
            if (recipes.HasCookingRecipe(recipe))
                continue;

            if (recipes.EnsureCookingRecipeLearned(recipe))
                ledger.CookingRecipes.Add(recipe);
        }
    }

    private static void ApplyCompletionDays(DesiredEffects desired, ProgressionSaveData saveData, int dayNumber, ModConfig config)
    {
        foreach (var questId in desired.CompletedQuests)
            EnsureCompletionDay(saveData, questId, dayNumber);

        foreach (var orderId in desired.CompletedSpecialOrders)
            EnsureCompletionDay(saveData, orderId, dayNumber);

        void EnsureCompletionDay(ProgressionSaveData data, string id, int nowDay)
        {
            var wait = ProgressionRules.GetDaysToWait(id);
            var desiredDay = nowDay - wait;
            if (!data.QuestCompletedDay.TryGetValue(id, out var existing))
            {
                data.QuestCompletedDay[id] = desiredDay;
                return;
            }

            // Keep the earliest day so we don't accidentally re-lock content.
            if (existing > desiredDay)
                data.QuestCompletedDay[id] = desiredDay;
        }
    }

    private static void ReconcileCompletionDays(DesiredEffects desired, ProgressionSaveData saveData, ProgressionEffectsLedger ledger)
    {
        var tracked = ledger.CompletedQuests.Concat(ledger.CompletedSpecialOrders).Distinct(StringComparer.Ordinal);
        foreach (var id in tracked)
        {
            if (desired.CompletedQuests.Contains(id) || desired.CompletedSpecialOrders.Contains(id))
                continue;

            saveData.QuestCompletedDay.Remove(id);
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

        foreach (var id in ledger.ActiveQuests.Where(id => !desired.ActiveQuests.Contains(id)).ToList())
        {
            if (quests.RemoveQuest(id))
                ledger.ActiveQuests.Remove(id);
        }

        foreach (var id in ledger.CompletedQuests.Where(id => !desired.CompletedQuests.Contains(id)).ToList())
        {
            if (quests.UncompleteQuest(id))
                ledger.CompletedQuests.Remove(id);
        }

        foreach (var id in ledger.CompletedSpecialOrders.Where(id => !desired.CompletedSpecialOrders.Contains(id)).ToList())
        {
            if (quests.UnmarkSpecialOrderCompleted(id))
                ledger.CompletedSpecialOrders.Remove(id);
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
        public HashSet<string> ActiveQuests { get; } = new(StringComparer.Ordinal);
        public HashSet<string> CompletedQuests { get; } = new(StringComparer.Ordinal);
        public HashSet<string> CompletedSpecialOrders { get; } = new(StringComparer.Ordinal);
        public HashSet<string> CraftingRecipes { get; } = new(StringComparer.Ordinal);
        public HashSet<string> CookingRecipes { get; } = new(StringComparer.Ordinal);
    }
}
