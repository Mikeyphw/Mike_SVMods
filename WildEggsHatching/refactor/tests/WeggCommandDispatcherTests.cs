using System;
using System.Collections.Generic;
using WildEggsHatching.Application.Facade;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Commands;
using WildEggsHatching.Infrastructure.Config;
using Xunit;

namespace WildEggsHatching.Refactor.Tests;

public sealed class WeggCommandDispatcherTests
{
    [Fact]
    public void Help_WhenNoArgs()
    {
        var facade = new FakeFacade();
        var dispatcher = new WeggCommandDispatcher(facade);

        var res = dispatcher.Execute(Array.Empty<string>());

        Assert.True(res.Success);
        Assert.Contains("wegg step get", res.Message);
    }

    [Fact]
    public void StepGet_UsesFacade()
    {
        var facade = new FakeFacade { Step = ProgressionStep.PartIV };
        var dispatcher = new WeggCommandDispatcher(facade);

        var res = dispatcher.Execute(new[] { "step", "get" });

        Assert.True(res.Success);
        Assert.Contains("PartIV", res.Message);
    }

    [Fact]
    public void StepSet_ParsesEnum_AndCallsFacade()
    {
        var facade = new FakeFacade();
        var dispatcher = new WeggCommandDispatcher(facade);

        var res = dispatcher.Execute(new[] { "step", "set", "PartIII" });

        Assert.True(res.Success);
        Assert.Equal(ProgressionStep.PartIII, facade.LastSetStep);
    }

    
    [Fact]
    public void StepSet_WithEffects_FlagsFacade()
    {
        var facade = new FakeFacade();
        var dispatcher = new WeggCommandDispatcher(facade);

        var res = dispatcher.Execute(new[] { "step", "set", "PartII", "--effects" });

        Assert.True(res.Success);
        Assert.True(facade.LastApplySideEffects);
    }

    [Fact]
    public void SpawnPlace_ParsesArgs_AndCallsFacade()
    {
        var facade = new FakeFacade();
        var dispatcher = new WeggCommandDispatcher(facade);

        var res = dispatcher.Execute(new[] { "spawn", "place", "Farm", "(O)390", "2", "50" });

        Assert.True(res.Success);
        Assert.Equal(("Farm", "(O)390", 2, 50), facade.LastPlace);
    }

    [Fact]
    public void InvalidStep_ReturnsFailure()
    {
        var facade = new FakeFacade();
        var dispatcher = new WeggCommandDispatcher(facade);

        var res = dispatcher.Execute(new[] { "step", "set", "Nope" });

        Assert.False(res.Success);
    }

    private sealed class FakeFacade : IModFacade
    {
        public ProgressionStep Step { get; set; } = ProgressionStep.PartI;

        public ProgressionStep? LastSetStep { get; private set; }
        public (string loc, string item, int count, int attempts)? LastPlace { get; private set; }

        public ProgressionStep GetProgressionStep() => Step;

        public ModConfig GetConfigSnapshot() => new();

        public IReadOnlyDictionary<string, int> GetQuestCompletionDays() => new Dictionary<string, int>();

        public FacadeResult ReloadAll() => new(true, "reloaded");

        public FacadeResult TrySetProgressionStep(ProgressionStep step, bool applySideEffects, string reason)
        {
            LastSetStep = step;
            LastApplySideEffects = applySideEffects;
            Step = step;
            return new FacadeResult(true, "ok");
        }

        public FacadeResult TryPlaceItem(string locationName, string qualifiedItemId, int count, int maxAttempts)
        {
            LastPlace = (locationName, qualifiedItemId, count, maxAttempts);
            return new FacadeResult(true, "ok");
        }

        public FacadeResult TryRunSpawnPlanNow() => new(true, "ok");
    }
}
