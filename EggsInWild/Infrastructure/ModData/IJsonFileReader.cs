namespace EggsInWild.Infrastructure.ModData;

public interface IJsonFileReader
{
    /// <summary>Read a JSON file relative to the mod folder.</summary>
    T? ReadJsonFile<T>(string relativePath) where T : class;

    /// <summary>Read raw file contents for validation/diagnostics.</summary>
    string? ReadText(string relativePath);
}
