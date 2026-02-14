using System;
using System.Collections.Generic;
using System.Text.Json;
using StardewModdingAPI;
using WildEggsHatching.Infrastructure.Logging;
using WildEggsHatching.Infrastructure.ModData;
using WildEggsHatching.Infrastructure.ModData.Models;
using Xunit;

namespace WildEggsHatching.Refactor.Tests;

public sealed class ModDataLoaderTests
{
    [Fact]
    public void Missing_File_Uses_Defaults_And_Logs()
    {
        var reader = new InMemoryJsonFileReader();
        var log = new CapturingLog();

        var loader = new ModDataLoader(reader, log);
        var snapshot = loader.Load();

        Assert.Empty(snapshot.WildAnimals);
        Assert.Empty(snapshot.LocationPacks);
        Assert.NotNull(snapshot.SpawnParameters);

        Assert.Contains(log.Messages, m => m.Level == LogLevel.Warn && m.Message.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Unknown_Fields_Do_Not_Crash()
    {
        var reader = new InMemoryJsonFileReader();
        var log = new CapturingLog();

        reader.Add(ModDataPaths.SpawnParameters, """{ "MaxSpawningTries": 123, "UnknownField": 999 }""");
        reader.Add(ModDataPaths.WildAnimalsList, """{ "Chicken": { "Price": 5, "ExtraField": 1 } }""");
        reader.Add(ModDataPaths.SpawnAreas, """{}""");
        reader.Add(ModDataPaths.LocationPacks, """{}""");

        var loader = new ModDataLoader(reader, log);
        var snapshot = loader.Load();

        Assert.Equal(123, snapshot.SpawnParameters.MaxSpawningTries);
        Assert.True(snapshot.WildAnimals.ContainsKey("Chicken"));
        Assert.Equal(5, snapshot.WildAnimals["Chicken"].Price);
    }

    [Fact]
    public void Default_Values_Are_Applied_When_Fields_Missing()
    {
        var reader = new InMemoryJsonFileReader();
        var log = new CapturingLog();

        reader.Add(ModDataPaths.WildAnimalsList, """{ "Chicken": { } }""");
        reader.Add(ModDataPaths.SpawnAreas, """{}""");
        reader.Add(ModDataPaths.SpawnParameters, """{}""");
        reader.Add(ModDataPaths.LocationPacks, """{}""");

        var loader = new ModDataLoader(reader, log);
        var snapshot = loader.Load();

        var chicken = snapshot.WildAnimals["Chicken"];
        Assert.False(chicken.LargeEgg);
        Assert.False(chicken.IsMagic);
        Assert.Equal(0, chicken.Price);
    }

    private sealed class InMemoryJsonFileReader : IJsonFileReader
    {
        private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);
        private readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public void Add(string path, string json) => _files[path] = json;

        public T? ReadJsonFile<T>(string relativePath) where T : class
        {
            if (!_files.TryGetValue(relativePath, out var json))
                return null;

            return JsonSerializer.Deserialize<T>(json, _opts);
        }

        public string? ReadText(string relativePath)
            => _files.TryGetValue(relativePath, out var txt) ? txt : null;
    }

    private sealed class CapturingLog : ILog
    {
        public sealed record Entry(string Message, LogLevel Level);

        public List<Entry> Messages { get; } = new();

        public void Log(string message, LogLevel level = LogLevel.Debug)
            => Messages.Add(new Entry(message, level));
    }
}
