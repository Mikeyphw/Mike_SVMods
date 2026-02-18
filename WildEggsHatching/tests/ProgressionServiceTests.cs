using WildEggsHatching.Application.Progression;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;
using WildEggsHatching.Infrastructure.Config;
using Xunit;

namespace WildEggsHatching.Refactor.Tests;

public sealed class ProgressionServiceTests
{
    [Fact]
    public void ProgressionModeDisabled_OnSaveLoaded_AutoLearnsRecipes_And_DoesNotMutateStep()
    {
        var store = new InMemorySaveDataStore();
        store.WriteEnvelope(
            SaveKeys.Progression,
            new SaveDataEnvelope<ProgressionSaveData>(version: 1, new ProgressionSaveData { Step = ProgressionStep.PartII })
        );

        var config = new FakeConfigProvider(new ModConfig { ProgressionMode = false });
        var facts = new FakeUnlockFactsProvider { DayNumber = 5, IsMainPlayer = true };
        var quests = new FakeQuestService();
        var mail = new FakeMailService();
        var recipes = new FakeRecipeService();
        var log = new FakeLog();

        var stepChangedCount = 0;

        var svc = new ProgressionService(store, config, facts, quests, mail, recipes, log, invalidateAssets: () => { });
        svc.StepChanged += _ => stepChangedCount++;

        svc.OnSaveLoaded();

        Assert.Equal(ProgressionStep.PartVI, svc.GetCurrentStep());
        Assert.Contains("Elixir Of Revitalized Essence", recipes.Learned);
        Assert.Contains("Wild Egg Incubator", recipes.Learned);
        Assert.Empty(mail.Tomorrow);
        Assert.Empty(mail.Received);
        Assert.Equal(0, stepChangedCount);

        svc.SaveNow();

        var envelope = store.ReadEnvelope<ProgressionSaveData>(SaveKeys.Progression);
        Assert.NotNull(envelope);
        Assert.NotNull(envelope!.Data);
        Assert.Equal(ProgressionStep.PartII, envelope.Data!.Step);
    }

    [Fact]
    public void DoesNotAdvanceWithoutRequiredMail()
    {
        var store = new InMemorySaveDataStore();
        var config = new FakeConfigProvider(new ModConfig { ProgressionMode = true });
        var facts = new FakeUnlockFactsProvider { DayNumber = 5, IsMainPlayer = true };
        var quests = new FakeQuestService();
        var mail = new FakeMailService();
        var recipes = new FakeRecipeService();
        var log = new FakeLog();

        var svc = new ProgressionService(store, config, facts, quests, mail, recipes, log, invalidateAssets: () => { });

        svc.OnSaveLoaded();
        svc.OnDayStarted();

        Assert.Equal(ProgressionStep.PartI, svc.GetCurrentStep());
        Assert.Empty(mail.Tomorrow);
    }

    [Fact]
    public void AdvancesToPartII_AndIsIdempotentSameDay()
    {
        var store = new InMemorySaveDataStore();
        var config = new FakeConfigProvider(new ModConfig { ProgressionMode = true });
        var facts = new FakeUnlockFactsProvider { DayNumber = 10, IsMainPlayer = true };
        facts.ReceivedMail[ProgressionIds.Mail.SpecialOrderDoneI] = true;

        var quests = new FakeQuestService();
        var mail = new FakeMailService();
        var recipes = new FakeRecipeService();
        var log = new FakeLog();

        var svc = new ProgressionService(store, config, facts, quests, mail, recipes, log, invalidateAssets: () => { });

        svc.OnSaveLoaded();
        svc.OnDayStarted();

        Assert.Equal(ProgressionStep.PartII, svc.GetCurrentStep());
        Assert.Contains(mail.Tomorrow, x => x.Id == ProgressionIds.Mail.MarnieThanks);

        // Replay-safe: multiple evaluations in the same day shouldn't advance again or duplicate mail.
        svc.OnDayStarted();
        Assert.Equal(1, mail.Tomorrow.FindAll(x => x.Id == ProgressionIds.Mail.MarnieThanks).Count);
    }

    [Fact]
    public void SkipsPartV_WhenDesertTombDisabled()
    {
        var store = new InMemorySaveDataStore();
        store.WriteEnvelope(
            SaveKeys.Progression,
            new WildEggsHatching.Infrastructure.SaveData.SaveDataEnvelope<ProgressionSaveData>(
                1,
                new ProgressionSaveData { Step = ProgressionStep.PartIV }
            )
        );

        var config = new FakeConfigProvider(new ModConfig { ProgressionMode = true, DesertTombModuleEnabled = false });
        var facts = new FakeUnlockFactsProvider { DayNumber = 20, IsMainPlayer = true };
        var quests = new FakeQuestService();
        var mail = new FakeMailService();
        var recipes = new FakeRecipeService();
        var log = new FakeLog();

        var svc = new ProgressionService(store, config, facts, quests, mail, recipes, log, invalidateAssets: () => { });

        svc.OnSaveLoaded();
        svc.OnDayStarted();

        Assert.Equal(ProgressionStep.PartVI, svc.GetCurrentStep());
        Assert.DoesNotContain(mail.Received, x => x.Id == ProgressionIds.Mail.WizardFindings);
        Assert.Contains(mail.Received, x => x.Id == ProgressionIds.Mail.MarnieGotIncubator);
    }

    [Fact]
    public void ClientDoesNotAdvanceOrWriteSaveData()
    {
        var store = new InMemorySaveDataStore();
        var config = new FakeConfigProvider(new ModConfig { ProgressionMode = true });
        var facts = new FakeUnlockFactsProvider { DayNumber = 10, IsMainPlayer = false };
        facts.ReceivedMail[ProgressionIds.Mail.SpecialOrderDoneI] = true;

        var quests = new FakeQuestService();
        var mail = new FakeMailService();
        var recipes = new FakeRecipeService();
        var log = new FakeLog();

        var svc = new ProgressionService(store, config, facts, quests, mail, recipes, log, invalidateAssets: () => { });

        svc.OnSaveLoaded();
        svc.OnDayStarted();
        svc.OnDayEnding();

        Assert.Equal(ProgressionStep.PartI, svc.GetCurrentStep());
        Assert.Empty(mail.Tomorrow);
        Assert.False(store.ContainsKey(SaveKeys.Progression));
    }

    [Fact]
    public void AdvancesToPartIII_WhenQuestCompleteAndWaitSatisfied()
    {
        var store = new InMemorySaveDataStore();
        store.WriteEnvelope(
            SaveKeys.Progression,
            new WildEggsHatching.Infrastructure.SaveData.SaveDataEnvelope<ProgressionSaveData>(
                1,
                new ProgressionSaveData
                {
                    Step = ProgressionStep.PartII,
                    QuestCompletedDay =
                    {
                        [ProgressionIds.Quests.WizardStudy] = 8
                    }
                }
            )
        );

        var config = new FakeConfigProvider(new ModConfig { ProgressionMode = true });
        var facts = new FakeUnlockFactsProvider { DayNumber = 10, IsMainPlayer = true };
        facts.CompletedQuests[ProgressionIds.Quests.WizardStudy] = true;

        var quests = new FakeQuestService();
        quests.SetCompletedQuest(ProgressionIds.Quests.WizardStudy, true);

        var mail = new FakeMailService();
        var recipes = new FakeRecipeService();
        var log = new FakeLog();

        var svc = new ProgressionService(store, config, facts, quests, mail, recipes, log, invalidateAssets: () => { });

        svc.OnSaveLoaded();
        svc.OnDayStarted();

        Assert.Equal(ProgressionStep.PartIII, svc.GetCurrentStep());
        Assert.Contains(mail.Received, x => x.Id == ProgressionIds.Mail.WizardDone && x.NoLetter);
    }

    

    [Fact]
    public void ForceSetStep_WithEffects_AppliesMailOnce_AndIsIdempotent()
    {
        var store = new InMemorySaveDataStore();
        var config = new FakeConfigProvider(new ModConfig { ProgressionMode = true });
        var facts = new FakeUnlockFactsProvider { DayNumber = 10, IsMainPlayer = true };
        var quests = new FakeQuestService();
        var mail = new FakeMailService();
        var recipes = new FakeRecipeService();
        var log = new FakeLog();

        var svc = new ProgressionService(store, config, facts, quests, mail, recipes, log, invalidateAssets: () => { });

        svc.OnSaveLoaded();

        Assert.True(svc.TryForceSetStep(ProgressionStep.PartII, applySideEffects: true, reason: "test", out var msg1));
        Assert.Contains(mail.Tomorrow, x => x.Id == ProgressionIds.Mail.MarnieThanks);

        Assert.True(svc.TryForceSetStep(ProgressionStep.PartII, applySideEffects: true, reason: "test", out var msg2));
        Assert.Contains("No-op", msg2);

        Assert.Equal(1, mail.Tomorrow.FindAll(x => x.Id == ProgressionIds.Mail.MarnieThanks).Count);
    }

    [Fact]
    public void ForceSetStep_RollbackAndReapplyEffects_DoesNotDuplicateReceivedMail()
    {
        var store = new InMemorySaveDataStore();
        var config = new FakeConfigProvider(new ModConfig { ProgressionMode = true });
        var facts = new FakeUnlockFactsProvider { DayNumber = 10, IsMainPlayer = true };
        var quests = new FakeQuestService();
        var mail = new FakeMailService();
        var recipes = new FakeRecipeService();
        var log = new FakeLog();

        var svc = new ProgressionService(store, config, facts, quests, mail, recipes, log, invalidateAssets: () => { });

        svc.OnSaveLoaded();

        Assert.True(svc.TryForceSetStep(ProgressionStep.PartIII, applySideEffects: true, reason: "test", out _));
        Assert.Contains(mail.Received, x => x.Id == ProgressionIds.Mail.WizardDone && x.NoLetter);

        Assert.True(svc.TryForceSetStep(ProgressionStep.PartII, applySideEffects: false, reason: "rollback", out _));
        Assert.True(svc.TryForceSetStep(ProgressionStep.PartIII, applySideEffects: true, reason: "reapply", out _));

        Assert.Equal(1, mail.Received.FindAll(x => x.Id == ProgressionIds.Mail.WizardDone).Count);
    }
}
