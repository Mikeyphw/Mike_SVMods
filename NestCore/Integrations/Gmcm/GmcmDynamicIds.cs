namespace Mikew.NestCore.Integrations.Gmcm;

/// <summary>
/// Helpers for producing stable element IDs from dynamic keys (dictionary editors).
/// </summary>
public static class GmcmDynamicIds
{
    /// <summary>
    /// Build a stable element id by normalizing <paramref name="key"/> and prefixing it with <paramref name="elementPrefix"/>.
    /// </summary>
    public static string ElementId(string elementPrefix, string key)
    {
        var normalizedKey = GmcmFieldIds.Normalize(key);
        if (string.IsNullOrWhiteSpace(elementPrefix))
            return normalizedKey;

        return $"{elementPrefix}.{normalizedKey}";
    }
}
