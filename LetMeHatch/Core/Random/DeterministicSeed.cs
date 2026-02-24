using System;
using System.Text;

namespace LetMeHatch.Core.Random;

/// <summary>
/// Stable deterministic seed helper for reproducible RNG across runs/tests.
/// </summary>
public static class DeterministicSeed
{
    public static int For(params object?[] parts)
    {
        var sb = new StringBuilder();
        foreach (var p in parts)
        {
            sb.Append(p?.ToString() ?? "null");
            sb.Append('|');
        }

        return Fnv1a32(sb.ToString());
    }

    /// <summary>Stable 32-bit hash (FNV-1a) for deterministic seeding across runs.</summary>
    public static int Fnv1a32(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        unchecked
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;

            uint hash = offset;
            foreach (var ch in text)
            {
                hash ^= ch;
                hash *= prime;
            }

            // Random expects non-negative int.
            return (int)(hash & 0x7FFFFFFF);
        }
    }
}
