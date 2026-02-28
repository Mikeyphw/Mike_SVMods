using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mikew.NestCore.Helpers;
using StardewModdingAPI;

namespace LetMeHatch.Infrastructure.Smapi;

/// <summary>
/// Reads JSON either from disk (Dev) or from the generated embedded store (non-Dev).
/// </summary>
internal static class EmbeddedJson
{
#if !LMH_DEV
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() },
    };
#endif

    internal static T? ReadOrNull<T>(IModHelper helper, string relativePath)
        where T : class
    {
        if (helper is null) throw new ArgumentNullException(nameof(helper));

#if LMH_DEV
        return helper.Data.ReadJsonFile<T>(relativePath);
#else
        var json = EmbeddedTextStore.GetOrThrow(
            LetMeHatch.Generated.EmbeddedTextFiles.Files,
            relativePath,
            context: "LetMeHatch embedded JSON"
        );

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to deserialize embedded JSON: {EmbeddedTextStore.NormalizeKey(relativePath)}", ex);
        }
#endif
    }

    internal static T ReadOrThrow<T>(IModHelper helper, string relativePath)
        where T : class
    {
        var value = ReadOrNull<T>(helper, relativePath);
        if (value is not null)
            return value;

        throw new InvalidOperationException($"Missing or empty JSON: {EmbeddedTextStore.NormalizeKey(relativePath)}");
    }

#if !LMH_DEV
    internal static string GetTextOrThrow(string relativePath, string? context = null)
        => EmbeddedTextStore.GetOrThrow(LetMeHatch.Generated.EmbeddedTextFiles.Files, relativePath, context);
#endif
}
