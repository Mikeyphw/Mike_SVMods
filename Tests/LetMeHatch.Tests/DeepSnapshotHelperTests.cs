// Mike_SVMods/Tests/LetMeHatch.Tests/DeepSnapshotHelperTests.cs
using System.Collections.Generic;
using LetMeHatch.Tests.TestHelpers;
using Xunit;

namespace LetMeHatch.Tests;

public sealed class DeepSnapshotHelperTests
{
    private sealed class Node
    {
        public string Name { get; set; } = "";
        public List<int> Values { get; } = new();
        public Dictionary<string, Node> Children { get; } = new();
    }

    [Fact]
    public void AssertNoDeepMutation_Detects_Nested_Collection_Mutation()
    {
        var root = new Node { Name = "root" };
        root.Values.Add(1);
        root.Children["a"] = new Node { Name = "a" };

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            AssertEx.AssertNoDeepMutation(root, () => root.Children["a"].Values.Add(9), maxDepth: 8));
    }

    [Fact]
    public void AssertNoDeepMutation_Passes_When_No_Changes()
    {
        var root = new Node { Name = "root" };
        root.Values.Add(1);
        root.Children["a"] = new Node { Name = "a" };

        AssertEx.AssertNoDeepMutation(root, () => { });
    }
}
