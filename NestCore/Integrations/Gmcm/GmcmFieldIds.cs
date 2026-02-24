using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Mikew.NestCore.Integrations.Gmcm;

/// <summary>
/// Stable field-id factory for GMCM fields.
/// The IDs are human-readable and deterministic, which makes it easy to:
/// - search for fields by ID;
/// - react to changes using GMCM's OnFieldChanged hook (if you use it elsewhere).
/// </summary>
public static class GmcmFieldIds
{
    private static readonly Regex UnsafeChars = new(@"[^A-Za-z0-9._\-:/]", RegexOptions.Compiled);

    /// <summary>
    /// Build a stable field id for a control.
    /// </summary>
    /// <remarks>
    /// Output format:
    /// <c>&lt;prefix&gt;.gmcm.&lt;pageId&gt;.&lt;elementId&gt;</c>
    /// </remarks>
    public static string Build(string prefix, string pageId, string elementId)
    {
        var raw = $"{prefix}.gmcm.{pageId}.{elementId}";
        var normalized = Normalize(raw);

        // Keep ids short-ish to avoid any downstream surprises.
        // If it's long, keep a readable prefix and append a stable hash.
        const int maxLen = 160;
        if (normalized.Length <= maxLen)
            return normalized;

        var hash = Sha1Hex(normalized);
        var keep = Math.Max(40, maxLen - (hash.Length + 1));
        return $"{normalized[..keep]}.{hash}";
    }

    /// <summary>Normalize a candidate id into a safe, compact identifier.</summary>
    public static string Normalize(string value)
    {
        value = value.Trim();
        value = UnsafeChars.Replace(value, "_");
        value = Regex.Replace(value, "_{2,}", "_");
        return value;
    }

    private static string Sha1Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA1.HashData(bytes);

        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }
}
