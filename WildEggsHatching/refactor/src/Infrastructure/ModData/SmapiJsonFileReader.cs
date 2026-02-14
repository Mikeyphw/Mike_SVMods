using System.IO;
using StardewModdingAPI;

namespace WildEggsHatching.Infrastructure.ModData;

public sealed class SmapiJsonFileReader : IJsonFileReader
{
    private readonly IModHelper _helper;

    public SmapiJsonFileReader(IModHelper helper)
    {
        _helper = helper;
    }

    public T? ReadJsonFile<T>(string relativePath) where T : class
        => _helper.Data.ReadJsonFile<T>(relativePath);

    public string? ReadText(string relativePath)
    {
        var full = Path.Combine(_helper.DirectoryPath, relativePath);
        return File.Exists(full) ? File.ReadAllText(full) : null;
    }
}
