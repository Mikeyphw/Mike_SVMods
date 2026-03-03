using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EggsInWild.Infrastructure.DesertTomb.Loot.Definitions;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Definitions;
using Xunit;

namespace EggsInWild.Tests;

public sealed class TombDropTableCoverageTests
{
    [Fact]
    public void EveryMonsterDefinition_ReferencesAnExistingDropTable()
    {
        var (monsterDefsJson, dropTablesJson) = ReadAssets();

        var defs = Deserialize<Dictionary<string, TombMonsterDefinitionJson>>(monsterDefsJson);
        var tables = Deserialize<Dictionary<string, TombDropTableDefinitionJson>>(dropTablesJson);

        Assert.NotNull(defs);
        Assert.NotEmpty(defs!);

        Assert.NotNull(tables);
        Assert.NotEmpty(tables!);

        foreach (var (monsterKey, def) in defs!)
        {
            Assert.NotNull(def);

            Assert.False(
                string.IsNullOrWhiteSpace(def!.DropTableId),
                $"MonsterDefinitions entry '{monsterKey}' is missing DropTableId."
            );

            var id = def.DropTableId!.Trim();
            Assert.True(
                tables!.ContainsKey(id),
                $"MonsterDefinitions entry '{monsterKey}' references missing DropTableId '{id}'."
            );
        }
    }

    private static (string MonsterDefinitionsJson, string DropTablesJson) ReadAssets()
    {
        var baseDir = AppContext.BaseDirectory;
        var current = new DirectoryInfo(baseDir);

        for (var i = 0; i < 20 && current is not null; i++)
        {
            // If tests are run from repo root or a build output folder, handle both shapes:
            // - <root>/EggsInWild/refactor/assets/...
            // - <root>/assets/... (if copied)
            var candidateA = Path.Combine(current.FullName, "EggsInWild", "refactor", "assets", "DesertTomb", "MonsterDefinitions.json");
            var candidateB = Path.Combine(current.FullName, "assets", "DesertTomb", "MonsterDefinitions.json");

            var defsPath = File.Exists(candidateA) ? candidateA : (File.Exists(candidateB) ? candidateB : null);
            if (defsPath is not null)
            {
                var dropTablesPath = Path.Combine(Path.GetDirectoryName(defsPath)!, "DropTables.json");

                if (!File.Exists(dropTablesPath))
                    throw new FileNotFoundException($"Found MonsterDefinitions.json at '{defsPath}', but DropTables.json was missing at '{dropTablesPath}'.");

                return (File.ReadAllText(defsPath), File.ReadAllText(dropTablesPath));
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate DesertTomb assets. Expected 'EggsInWild/refactor/assets/DesertTomb/MonsterDefinitions.json' somewhere above the test output directory."
        );
    }

    private static T? Deserialize<T>(string json) where T : class
        => JsonSerializer.Deserialize<T>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }
        );
}
