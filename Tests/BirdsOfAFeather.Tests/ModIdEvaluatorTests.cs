using System;
using Mikew.BirdsOfAFeather.Helpers;
using Xunit;

namespace BirdsOfAFeather.Tests;

public class ModIdEvaluatorTests
{
    [Fact]
    public void Vanilla_IsAlwaysTrue()
    {
        Assert.True(ModIdEvaluator.Evaluate("vanilla", _ => false));
    }

    [Fact]
    public void Precedence_AndBeforeOr()
    {
        // A || (B && C)
        bool isLoaded(string id) => id switch
        {
            "A" => true,
            "B" => false,
            "C" => false,
            _ => false,
        };

        Assert.True(ModIdEvaluator.Evaluate("A || B && C", isLoaded));
    }

    [Fact]
    public void Parentheses_Work()
    {
        bool isLoaded(string id) => id is "A" or "B";
        Assert.True(ModIdEvaluator.Evaluate("(A && B) || C", isLoaded));
        Assert.False(ModIdEvaluator.Evaluate("A && (B || C)", id => id is "A"));
    }

    [Fact]
    public void Whitespace_Ignored()
    {
        bool isLoaded(string id) => id is "A" && true;
        Assert.True(ModIdEvaluator.Evaluate("  ( A  )  ", isLoaded));
    }
}
