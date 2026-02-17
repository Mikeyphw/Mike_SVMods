using System;
using System.Collections.Generic;
using RoosterAddon.Integrations;
using Xunit;
using Mikew.BirdsOfAFeather.Framework;

namespace RoosterAddon.Tests;

public sealed class LetMeHatchApiAdapterTests
{
    private sealed class FakeApi
    {
        public List<string> AnimalsWithClonedEggs { get; } = new() { "Duck" };
        public bool FinishedEarlyStuff { get; } = true;

        public int EDIT_BIRTH_TEXT_PRIORITY { get; } = 1;
        public int EDIT_EGG_LIST_PRIORITY { get; } = 2;
        public int CLONED_EGGS_PRIORITY { get; } = 3;
        public int NEW_EGGS_ADD_PRIORITY { get; } = 4;

        public List<(string FemaleKey, string MaleKey, bool AddAsAlternatePurchase)> GetClonedAnimalsInfo() =>
            new() { ("Duck", "Drake", true) };
    }

    [Fact]
    public void Wrap_Returns_Null_When_Null()
    {
        Assert.Null(LetMeHatchApiAdapter.Wrap(null));
    }

    [Fact]
    public void Wrap_Adapts_Reflection_Api()
    {
        var api = LetMeHatchApiAdapter.Wrap(new FakeApi());
        Assert.NotNull(api);

        Assert.Equal(new List<string> { "Duck" }, api!.AnimalsWithClonedEggs);
        Assert.True(api.FinishedEarlyStuff);
        Assert.Equal(1, api.EDIT_BIRTH_TEXT_PRIORITY);
        Assert.Equal(2, api.EDIT_EGG_LIST_PRIORITY);
        Assert.Equal(3, api.CLONED_EGGS_PRIORITY);
        Assert.Equal(4, api.NEW_EGGS_ADD_PRIORITY);

        var cloned = api.GetClonedAnimalsInfo();
        Assert.Single(cloned);
        Assert.Equal(("Duck", "Drake", true), cloned[0]);
    }

    [Fact]
    public void Wrap_Returns_Same_When_Typed()
    {
        ILetMeHatchApi typed = new TypedApi();
        var wrapped = LetMeHatchApiAdapter.Wrap(typed);
        Assert.Same(typed, wrapped);
    }

    private sealed class TypedApi : ILetMeHatchApi
    {
        public List<string> AnimalsWithClonedEggs { get; } = new();
        public bool FinishedEarlyStuff { get; } = false;

        public int EDIT_BIRTH_TEXT_PRIORITY { get; } = 0;
        public int EDIT_EGG_LIST_PRIORITY { get; } = 0;
        public int CLONED_EGGS_PRIORITY { get; } = 0;
        public int NEW_EGGS_ADD_PRIORITY { get; } = 0;

        public List<(string FemaleKey, string MaleKey, bool AddAsAlternatePurchase)> GetClonedAnimalsInfo() => new();
    }
}
