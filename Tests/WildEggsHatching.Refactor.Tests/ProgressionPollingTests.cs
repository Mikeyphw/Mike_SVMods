using WildEggsHatching.Application.Progression;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;
using WildEggsHatching.Infrastructure.Config;
using Mikew.BirdsOfAFeather.Helpers.SaveData;
using Xunit;

namespace WildEggsHatching.Refactor.Tests;

public sealed class ProgressionPollingTests
{
    [Fact]
    public void Polling_SpecialOrderDoneMail_RecordsCompletionDay_And_AdvancesStep()
    {
        var store = new InMemorySaveDataStore();
        var config = new FakeConfigProvider(new ModConfig { ProgressionMode = true, DesertTombModuleEnabled = true });
        var facts = new FakeUnlockFactsProvider { DayNumber = 10, IsMainPlayer = true };
        var quests = new FakeQuestService();
        var mail = new FakeMailService();
        var recipes = new FakeRecipeService();
        var log = new FakeLog();

        var svc = new ProgressionService(store, config, facts, quests, mail, recipes, log, invalidateAssets: () => { });

        svc.OnSaveLoaded();

        Assert.Equal(ProgressionStep.PartI, svc.GetCurrentStep());

        // Simulate the SpecialOrder reward flag being set mid-day.
        facts.ReceivedMail[ProgressionIds.Mail.SpecialOrderDoneI] = true;
        mail.AddReceivedMail(ProgressionIds.Mail.SpecialOrderDoneI, noLetter: true);

        svc.OnUpdateTicked(isPollingTick: true);

        Assert.Equal(ProgressionStep.PartII, svc.GetCurrentStep());
        Assert.Contains(mail.Tomorrow, x => x.Id == ProgressionIds.Mail.MarnieThanks);

        svc.OnDayEnding();

        var envelope = store.ReadEnvelope<ProgressionSaveData>(SaveKeys.Progression);
        Assert.NotNull(envelope);
        Assert.True(envelope!.Data!.QuestCompletedDay.ContainsKey(ProgressionIds.Quests.SpecialOrderWildEggsI));
        Assert.Equal(10, envelope!.Data!.QuestCompletedDay[ProgressionIds.Quests.SpecialOrderWildEggsI]);
    }

    [Fact]
    public void Polling_QuestCompletion_SetsCompletionDay_And_WaitsBeforeAdvancing()
    {
        var store = new InMemorySaveDataStore();
        store.WriteEnvelope(
            SaveKeys.Progression,
            new SaveDataEnvelope<ProgressionSaveData>(
                1,
                new ProgressionSaveData { Step = ProgressionStep.PartII }
            )
        );

        var config = new FakeConfigProvider(new ModConfig { ProgressionMode = true, DesertTombModuleEnabled = true });
        var facts = new FakeUnlockFactsProvider { DayNumber = 10, IsMainPlayer = true };
        var quests = new FakeQuestService();
        var mail = new FakeMailService();
        var recipes = new FakeRecipeService();
        var log = new FakeLog();

        var svc = new ProgressionService(store, config, facts, quests, mail, recipes, log, invalidateAssets: () => { });

        svc.OnSaveLoaded();

        Assert.Equal(ProgressionStep.PartII, svc.GetCurrentStep());

        // Quest completes mid-day; polling detects and records completion day.
        quests.SetCompletedQuest(ProgressionIds.Quests.WizardStudy, true);
        facts.CompletedQuests[ProgressionIds.Quests.WizardStudy] = true;

        svc.OnUpdateTicked(isPollingTick: true);

        // Not enough days passed to unlock PartIII yet (wait=2).
        Assert.Equal(ProgressionStep.PartII, svc.GetCurrentStep());

        svc.OnDayEnding();
        var envelope = store.ReadEnvelope<ProgressionSaveData>(SaveKeys.Progression);
        Assert.NotNull(envelope);
        Assert.Equal(10, envelope!.Data!.QuestCompletedDay[ProgressionIds.Quests.WizardStudy]);

        // Advance time; day-start evaluation should now move to PartIII.
        facts.DayNumber = 12;
        svc.OnDayStarted();

        Assert.Equal(ProgressionStep.PartIII, svc.GetCurrentStep());
        Assert.Contains(mail.Received, x => x.Id == ProgressionIds.Mail.WizardDone && x.NoLetter);
    }
}
