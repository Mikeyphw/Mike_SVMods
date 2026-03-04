using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.ModData.Models;
using Xunit;

namespace EggsInWild.Tests;

public sealed class ExtraModAnimalKeysBulkParserAndPlannerTests
{
    private static readonly Type ModEntryType = typeof(EggsInWild.ModEntry);

    private static readonly MethodInfo ParseBulkMethod =
        ModEntryType.GetMethod("ParseExtraModAnimalKeysBulk", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("ParseExtraModAnimalKeysBulk not found (private static).");

    private static readonly MethodInfo ComputePlanMethod =
        ModEntryType.GetMethod("ComputeExtraModAnimalKeysBulkPlanCounts", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("ComputeExtraModAnimalKeysBulkPlanCounts not found (private static).");

    [Fact]
    public void ParseBulk_MissingSeparator_WarnsAndIgnoresLine()
    {
        var st = Parse("Author.Mod Cow, Pig");
        var warnings = GetWarnings(st);

        Assert.Contains(warnings, w => w.Contains("missing separator", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(GetUpserts(st));
        Assert.Empty(GetResets(st));
    }

    [Fact]
    public void ParseBulk_MultiModIds_UpsertsEachModId()
    {
        var st = Parse("A,B | Cow, Pig");
        var upserts = GetUpserts(st);

        Assert.True(upserts.ContainsKey("A"));
        Assert.True(upserts.ContainsKey("B"));

        Assert.Equal(new[] { "Cow", "Pig" }, upserts["A"]);
        Assert.Equal(new[] { "Cow", "Pig" }, upserts["B"]);
    }

    [Fact]
    public void ParseBulk_ResetRemovesUpsertAndAddsReset()
    {
        var st = Parse("A | Cow\nA | *");
        var upserts = GetUpserts(st);
        var resets = GetResets(st);

        Assert.False(upserts.ContainsKey("A"));
        Assert.Contains("A", resets, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComputePlan_NoOverride_UpsertMatchingBase_IsNoop()
    {
        var baseDef = new WildAnimalJson
        {
            ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = new ExtraModAnimalKeysJson { FarmAnimalKeys = new List<string> { "Cow" } },
            },
        };

        var st = Parse("A | Cow");
        var plan = ComputePlan(st, knownModIds: new[] { "A" }, definition: baseDef, ovNow: null, fixTrim: false, fixDedupe: false, knownFarmAnimalIds: null);

        Assert.Equal(0, plan.PlannedSet);
        Assert.Equal(0, plan.PlannedClear);
        Assert.Equal(1, plan.PlannedNoop);
        Assert.False(plan.AnyChangePlanned);
    }

    [Fact]
    public void ComputePlan_WithOverride_UpsertMatchingBase_ClearsOverride()
    {
        var baseDef = new WildAnimalJson
        {
            ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = new ExtraModAnimalKeysJson { FarmAnimalKeys = new List<string> { "Cow" } },
            },
        };

        var ovNow = CreateWildAnimalOverride(extraModOverrides: new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = new ExtraModAnimalKeysOverrideConfig { FarmAnimalKeys = new List<string> { "Pig" } },
        });

        var st = Parse("a | Cow"); // casing should canonicalize to known modId "A"
        var plan = ComputePlan(st, knownModIds: new[] { "A" }, definition: baseDef, ovNow: ovNow, fixTrim: false, fixDedupe: false, knownFarmAnimalIds: null);

        Assert.Equal(0, plan.PlannedSet);
        Assert.Equal(1, plan.PlannedClear);
        Assert.Equal(0, plan.PlannedNoop);
        Assert.True(plan.AnyChangePlanned);
    }

    [Fact]
    public void ComputePlan_NoOverride_UpsertDifferentFromBase_IsSet()
    {
        var baseDef = new WildAnimalJson
        {
            ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = new ExtraModAnimalKeysJson { FarmAnimalKeys = new List<string> { "Cow" } },
            },
        };

        var st = Parse("A | Pig");
        var plan = ComputePlan(st, knownModIds: new[] { "A" }, definition: baseDef, ovNow: null, fixTrim: false, fixDedupe: false, knownFarmAnimalIds: null);

        Assert.Equal(1, plan.PlannedSet);
        Assert.Equal(0, plan.PlannedClear);
        Assert.Equal(0, plan.PlannedNoop);
        Assert.True(plan.AnyChangePlanned);
    }

    private static object Parse(string raw, ISet<string>? knownModIds = null, ISet<string>? knownFarmAnimalIds = null)
    {
        return ParseBulkMethod.Invoke(null, new object?[] { raw, knownModIds, knownFarmAnimalIds })
               ?? throw new InvalidOperationException("ParseExtraModAnimalKeysBulk returned null.");
    }

    private static PlanCounts ComputePlan(
        object st,
        IEnumerable<string> knownModIds,
        WildAnimalJson? definition,
        object? ovNow,
        bool fixTrim,
        bool fixDedupe,
        ISet<string>? knownFarmAnimalIds)
    {
        var res = ComputePlanMethod.Invoke(
                      null,
                      new[]
                      {
                          st,
                          knownModIds,
                          definition,
                          ovNow,
                          fixTrim,
                          fixDedupe,
                          knownFarmAnimalIds,
                      })
                  ?? throw new InvalidOperationException("ComputeExtraModAnimalKeysBulkPlanCounts returned null.");

        return new PlanCounts(
            PlannedSet: GetIntProperty(res, "PlannedSet"),
            PlannedClear: GetIntProperty(res, "PlannedClear"),
            PlannedNoop: GetIntProperty(res, "PlannedNoop"),
            AnyChangePlanned: GetBoolProperty(res, "AnyChangePlanned"));
    }

    private static Dictionary<string, List<string>> GetUpserts(object state)
    {
        var prop = state.GetType().GetProperty("Upserts", BindingFlags.Instance | BindingFlags.Public)
                   ?? throw new InvalidOperationException("Upserts property missing.");

        var dictObj = prop.GetValue(state) ?? throw new InvalidOperationException("Upserts is null.");
        var dict = (IDictionary)dictObj;

        var outDict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry entry in dict)
        {
            var k = (string)entry.Key;
            var v = ((IEnumerable)entry.Value).Cast<string>().ToList();
            outDict[k] = v;
        }

        return outDict;
    }

    private static HashSet<string> GetResets(object state)
    {
        var prop = state.GetType().GetProperty("Resets", BindingFlags.Instance | BindingFlags.Public)
                   ?? throw new InvalidOperationException("Resets property missing.");

        var obj = prop.GetValue(state) ?? throw new InvalidOperationException("Resets is null.");
        return ((IEnumerable)obj).Cast<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static List<string> GetWarnings(object state)
    {
        var prop = state.GetType().GetProperty("Warnings", BindingFlags.Instance | BindingFlags.Public)
                   ?? throw new InvalidOperationException("Warnings property missing.");

        var obj = prop.GetValue(state) ?? throw new InvalidOperationException("Warnings is null.");
        return ((IEnumerable)obj).Cast<string>().ToList();
    }

    private static object CreateWildAnimalOverride(Dictionary<string, ExtraModAnimalKeysOverrideConfig> extraModOverrides)
    {
        var ovType = typeof(ModConfig).Assembly.GetType("EggsInWild.Infrastructure.Config.WildAnimalOverrideConfig")
                    ?? throw new InvalidOperationException("WildAnimalOverrideConfig type not found.");

        var ov = Activator.CreateInstance(ovType) ?? throw new InvalidOperationException("Failed creating WildAnimalOverrideConfig.");

        var prop = ovType.GetProperty("ExtraModAnimalKeys", BindingFlags.Instance | BindingFlags.Public)
                   ?? throw new InvalidOperationException("WildAnimalOverrideConfig.ExtraModAnimalKeys missing.");

        prop.SetValue(ov, extraModOverrides);

        return ov;
    }

    private static int GetIntProperty(object o, string name)
    {
        var prop = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
                   ?? throw new InvalidOperationException($"Property '{name}' missing on {o.GetType().FullName}.");

        return (int)(prop.GetValue(o) ?? 0);
    }

    private static bool GetBoolProperty(object o, string name)
    {
        var prop = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
                   ?? throw new InvalidOperationException($"Property '{name}' missing on {o.GetType().FullName}.");

        return (bool)(prop.GetValue(o) ?? false);
    }

    private readonly record struct PlanCounts(int PlannedSet, int PlannedClear, int PlannedNoop, bool AnyChangePlanned);
}
