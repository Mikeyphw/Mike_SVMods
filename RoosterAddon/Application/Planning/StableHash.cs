using System;
using System.Text;

namespace RoosterAddon.Application.Planning;

internal static class StableHash
{
    // FNV-1a 64-bit.
    public static ulong Fnv1a64(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        const ulong offset = 14695981039346656037;
        const ulong prime = 1099511628211;

        var hash = offset;
        var bytes = Encoding.UTF8.GetBytes(value);
        foreach (var b in bytes)
        {
            hash ^= b;
            hash *= prime;
        }

        return hash;
    }

    public static string Hex(ulong value) => value.ToString("x16");
}
