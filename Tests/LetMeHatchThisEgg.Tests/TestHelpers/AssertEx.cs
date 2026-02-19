// Mike_SVMods/Tests/LetMeHatchThisEgg.Tests/TestHelpers/AssertEx.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace LetMeHatchThisEgg.Tests.TestHelpers;

/// <summary>
/// Small assertion helpers for mutation checks.
/// </summary>
public static class AssertEx
{
    /// <summary>
    /// Asserts that <paramref name="action"/> does not mutate <paramref name="obj"/> (shallow snapshot).
    /// Use <see cref="AssertNoDeepMutation"/> when objects contain nested lists/dicts.
/// </summary>
    public static void AssertNoMutation(object obj, Action action, string? because = null)
    {
        var before = Snapshotter.ShallowSnapshot(obj);
        action();
        var after = Snapshotter.ShallowSnapshot(obj);

        Assert.True(before == after, BuildMessage("Expected no mutation (shallow).", because, before, after));
    }

    /// <summary>
    /// Asserts that <paramref name="action"/> does not mutate <paramref name="obj"/> recursively,
    /// including nested collections/dictionaries and nested objects.
    /// </summary>
    public static void AssertNoDeepMutation(object obj, Action action, string? because = null, int maxDepth = 6, int maxItemsPerCollection = 200)
    {
        var before = Snapshotter.DeepSnapshot(obj, maxDepth, maxItemsPerCollection);
        action();
        var after = Snapshotter.DeepSnapshot(obj, maxDepth, maxItemsPerCollection);

        Assert.True(before == after, BuildMessage("Expected no mutation (deep).", because, before, after));
    }

    public static void AssertSomePropertyMutation(object obj, Action action, string? because = null)
    {
        var before = Snapshotter.DeepSnapshot(obj, maxDepth: 3, maxItemsPerCollection: 200);
        action();
        var after = Snapshotter.DeepSnapshot(obj, maxDepth: 3, maxItemsPerCollection: 200);

        Assert.True(before != after, because ?? "Expected a mutation, but snapshots match.");
    }

    public static void MutateSomeWritableProperties(object obj)
    {
        Snapshotter.TryMutateWritableProperties(obj, maxDepth: 2);
    }

    private static string BuildMessage(string header, string? because, string before, string after)
    {
        var sb = new StringBuilder();
        sb.AppendLine(header);
        if (!string.IsNullOrWhiteSpace(because))
            sb.AppendLine($"Because: {because}");
        sb.AppendLine("---- BEFORE ----");
        sb.AppendLine(before);
        sb.AppendLine("---- AFTER ----");
        sb.AppendLine(after);
        return sb.ToString();
    }

    private static class Snapshotter
    {
        private static readonly BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static string ShallowSnapshot(object obj)
        {
            if (obj is null) return "<null>";
            var t = obj.GetType();
            var sb = new StringBuilder();
            sb.AppendLine(t.FullName ?? t.Name);

            foreach (var p in t.GetProperties(Flags).Where(IsReadableValueProperty).OrderBy(p => p.Name))
            {
                object? v;
                try { v = p.GetValue(obj); }
                catch { v = "<unreadable>"; }
                sb.AppendLine($"{p.Name}={FormatScalar(v)}");
            }

            foreach (var f in t.GetFields(Flags).Where(f => !f.IsStatic).OrderBy(f => f.Name))
            {
                object? v;
                try { v = f.GetValue(obj); }
                catch { v = "<unreadable>"; }
                sb.AppendLine($"{f.Name}={FormatScalar(v)}");
            }

            return sb.ToString();
        }

        public static string DeepSnapshot(object obj, int maxDepth, int maxItemsPerCollection)
        {
            var sb = new StringBuilder(4096);
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            WriteNode(sb, obj, depth: 0, maxDepth, maxItemsPerCollection, visited);
            return sb.ToString();
        }

        private static void WriteNode(StringBuilder sb, object? obj, int depth, int maxDepth, int maxItemsPerCollection, HashSet<object> visited)
        {
            var indent = new string(' ', depth * 2);

            if (obj is null)
            {
                sb.AppendLine($"{indent}<null>");
                return;
            }

            var t = obj.GetType();

            if (IsScalar(t))
            {
                sb.AppendLine($"{indent}{FormatScalar(obj)}");
                return;
            }

            if (!visited.Add(obj))
            {
                sb.AppendLine($"{indent}<cycle:{t.FullName}>");
                return;
            }

            if (depth >= maxDepth)
            {
                sb.AppendLine($"{indent}<{t.FullName}:maxDepth>");
                return;
            }

            // IDictionary
            if (obj is IDictionary dict)
            {
                sb.AppendLine($"{indent}{t.FullName} (dict)");
                var entries = new List<(string Key, object? Value)>();
                foreach (DictionaryEntry de in dict)
                    entries.Add((Key: FormatScalar(de.Key), Value: de.Value));

                foreach (var kv in entries.OrderBy(e => e.Key).Take(maxItemsPerCollection))
                {
                    sb.AppendLine($"{indent}  [{kv.Key}]");
                    WriteNode(sb, kv.Value, depth + 2, maxDepth, maxItemsPerCollection, visited);
                }
                return;
            }

            // IEnumerable (but not string)
            if (obj is IEnumerable enumerable && obj is not string)
            {
                sb.AppendLine($"{indent}{t.FullName} (list)");
                var i = 0;
                foreach (var item in enumerable)
                {
                    if (i++ >= maxItemsPerCollection)
                    {
                        sb.AppendLine($"{indent}  <truncated:{maxItemsPerCollection}>");
                        break;
                    }

                    sb.AppendLine($"{indent}  [{i - 1}]");
                    WriteNode(sb, item, depth + 2, maxDepth, maxItemsPerCollection, visited);
                }
                return;
            }

            sb.AppendLine($"{indent}{t.FullName}");
            foreach (var p in t.GetProperties(Flags).Where(IsReadableValueProperty).OrderBy(p => p.Name))
            {
                object? v;
                try { v = p.GetValue(obj); }
                catch { v = "<unreadable>"; }

                sb.AppendLine($"{indent}  {p.Name}:");
                WriteNode(sb, v, depth + 2, maxDepth, maxItemsPerCollection, visited);
            }

            foreach (var f in t.GetFields(Flags).Where(f => !f.IsStatic).OrderBy(f => f.Name))
            {
                object? v;
                try { v = f.GetValue(obj); }
                catch { v = "<unreadable>"; }

                sb.AppendLine($"{indent}  {f.Name}:");
                WriteNode(sb, v, depth + 2, maxDepth, maxItemsPerCollection, visited);
            }
        }

        public static void TryMutateWritableProperties(object obj, int maxDepth)
        {
            if (obj is null) return;
            TryMutate(obj, depth: 0, maxDepth, new HashSet<object>(ReferenceEqualityComparer.Instance));
        }

        private static void TryMutate(object obj, int depth, int maxDepth, HashSet<object> visited)
        {
            if (obj is null) return;
            if (!visited.Add(obj)) return;
            if (depth >= maxDepth) return;

            var t = obj.GetType();
            if (IsScalar(t)) return;

            foreach (var p in t.GetProperties(Flags).Where(IsWritableValueProperty))
            {
                var targetType = p.PropertyType;
                try
                {
                    var current = p.GetValue(obj);
                    var mutated = MutateScalar(current, targetType);
                    if (mutated is not null)
                    {
                        p.SetValue(obj, mutated);
                        return;
                    }
                }
                catch
                {
                    // best-effort only
                }
            }

            // Recurse into readable nodes
            foreach (var p in t.GetProperties(Flags).Where(IsReadableValueProperty))
            {
                try
                {
                    var v = p.GetValue(obj);
                    if (v is null) continue;
                    if (IsScalar(v.GetType())) continue;
                    TryMutate(v, depth + 1, maxDepth, visited);
                }
                catch { }
            }

            foreach (var f in t.GetFields(Flags).Where(f => !f.IsStatic))
            {
                try
                {
                    var v = f.GetValue(obj);
                    if (v is null) continue;
                    if (IsScalar(v.GetType())) continue;
                    TryMutate(v, depth + 1, maxDepth, visited);
                }
                catch { }
            }
        }

        private static object? MutateScalar(object? current, Type targetType)
        {
            if (targetType == typeof(string))
                return (current as string ?? string.Empty) + "_mut";

            if (targetType == typeof(int))
                return (current is int i ? i : 0) + 1;

            if (targetType == typeof(float))
                return (current is float f ? f : 0f) + 1f;

            if (targetType == typeof(double))
                return (current is double d ? d : 0d) + 1d;

            if (targetType == typeof(bool))
                return !(current is bool b && b);

            if (targetType.IsEnum)
            {
                var values = Enum.GetValues(targetType);
                return values.Length > 1 ? values.GetValue(1) : values.GetValue(0);
            }

            return null;
        }

        private static bool IsReadableValueProperty(PropertyInfo p)
        {
            if (!p.CanRead) return false;
            if (p.GetIndexParameters().Length != 0) return false;
            return true;
        }

        private static bool IsWritableValueProperty(PropertyInfo p)
        {
            if (!p.CanRead || !p.CanWrite) return false;
            if (p.GetIndexParameters().Length != 0) return false;
            return IsScalar(p.PropertyType);
        }

        private static bool IsScalar(Type t) =>
            t.IsPrimitive ||
            t.IsEnum ||
            t == typeof(string) ||
            t == typeof(decimal) ||
            t == typeof(DateTime) ||
            t == typeof(Guid);

        private static string FormatScalar(object? v)
        {
            if (v is null) return "<null>";
            if (v is string s) return $"\"{s}\"";
            return v.ToString() ?? "<nullstr>";
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new();

            public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
