#if !WEGG_DEV
using System.Text.Json;
using System.Text.Json.Serialization;
using EggsInWild.Generated;
using Mikew.NestCore.Helpers;

namespace EggsInWild.Infrastructure.ModData;

/// <summary>Reads JSON from the compiled embedded text store (non-Dev builds).</summary>
public sealed class EmbeddedJsonFileReader : IJsonFileReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() },
    };

    public T? ReadJsonFile<T>(string relativePath) where T : class
    {
        var json = EmbeddedTextStore.GetOrThrow(
            EmbeddedTextFiles.Files,
            relativePath,
            context: "Non-Dev build embeds JSON"
        );
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    public string? ReadText(string relativePath)
        => EmbeddedTextStore.TryGet(EmbeddedTextFiles.Files, relativePath, out var text) ? text : null;
}
#else
namespace EggsInWild.Infrastructure.ModData;

/// <summary>Dev builds read JSON from disk; this type should never be used.</summary>
public sealed class EmbeddedJsonFileReader : IJsonFileReader
{
    public T? ReadJsonFile<T>(string relativePath) where T : class
        => throw new System.InvalidOperationException("EmbeddedJsonFileReader is not available in Dev builds.");

    public string? ReadText(string relativePath)
        => throw new System.InvalidOperationException("EmbeddedJsonFileReader is not available in Dev builds.");
}
#endif
