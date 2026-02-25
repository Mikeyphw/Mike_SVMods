using System;
using System.Collections.Generic;

namespace Mikew.NestCore.Helpers;

/// <summary>Helpers for reading embedded text assets keyed by relative paths.</summary>
public static class EmbeddedTextStore
{
    public static string NormalizeKey(string relativePath)
        => (relativePath ?? string.Empty).Replace('\\', '/').TrimStart('/');

    public static bool TryGet(IReadOnlyDictionary<string, string> files, string relativePath, out string text)
    {
        if (files is null)
            throw new ArgumentNullException(nameof(files));

        return files.TryGetValue(NormalizeKey(relativePath), out text!);
    }

    public static string GetOrThrow(IReadOnlyDictionary<string, string> files, string relativePath, string? context = null)
    {
        if (TryGet(files, relativePath, out var text))
            return text;

        var msg = $"Embedded file missing: {NormalizeKey(relativePath)}";
        if (!string.IsNullOrWhiteSpace(context))
            msg += $" ({context})";
        throw new InvalidOperationException(msg);
    }
}
