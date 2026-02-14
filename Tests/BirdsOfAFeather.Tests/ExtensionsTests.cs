using System;
using System.Collections.Generic;
using Mikew.BirdsOfAFeather.Helpers;
using Xunit;

namespace BirdsOfAFeather.Tests;

public class ExtensionsTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("x", false)]
    public void IsEmptyOrWhitespace_Works(string? input, bool expected)
    {
        Assert.Equal(expected, input.IsEmptyOrWhitespace());
    }

    [Fact]
    public void TryAdd_AddsOnlyOnce()
    {
        var list = new List<int> { 1 };

        Assert.False(list.TryAdd(1));
        Assert.True(list.TryAdd(2));

        Assert.Equal(new[] { 1, 2 }, list);
    }

    [Fact]
    public void TryAdd_NullList_Throws()
    {
        List<int>? list = null;
        Assert.Throws<ArgumentNullException>(() => list!.TryAdd(1));
    }

    [Fact]
    public void GetRandomElementOrDefault_Empty_ReturnsDefault()
    {
        var list = new List<int>();
        var value = list.GetRandomElementOrDefault(new Random(123));
        Assert.Equal(default, value);
    }
}
