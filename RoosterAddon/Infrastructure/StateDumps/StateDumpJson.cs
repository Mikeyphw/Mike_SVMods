using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace RoosterAddon.Infrastructure.StateDumps;

internal static class StateDumpJson
{
    public static string BuildFileName(string prefix, DateTimeOffset timestamp) =>
        $"{prefix}_{timestamp:yyyyMMdd_HHmmss}.json";

    public static string SerializeIndented(object payload)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(payload, options);
    }

    public static void WriteUtf8(string directory, string fileName, string json)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, fileName), json, Encoding.UTF8);
    }
}
