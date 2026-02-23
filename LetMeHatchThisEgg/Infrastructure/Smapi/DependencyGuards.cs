using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI;

namespace LetMeHatchThisEgg.Infrastructure.Smapi;

public static class DependencyGuards
{
    public const string BirdsOfAFeatherId = "Mikew.birdsofafeather";

    public static void AssertBirdsOfAFeatherPresent(IModRegistry registry)
    {
        if (!registry.IsLoaded(BirdsOfAFeatherId))
        {
            throw new InvalidOperationException(
                $"LetMeHatchThisEgg requires BirdsOfAFeather ({BirdsOfAFeatherId}). " +
                "Install/enable it and try again."
            );
        }
    }

    public static void AssertNoLocalBirdsOfAFeatherCopies(string modDirectoryPath, IMonitor monitor)
    {
        var offenders = FindLocalBirdsOfAFeatherCopies(modDirectoryPath);

        if (offenders.Count == 0)
        {
            return;
        }

        monitor.Log(
            "Found local BirdsOfAFeather copies inside LetMeHatchThisEgg. " +
            "BirdsOfAFeather must be installed as its own mod; remove embedded copies.",
            LogLevel.Error
        );

        foreach (var offender in offenders)
        {
            monitor.Log($"  - {offender}", LogLevel.Error);
        }

        throw new InvalidOperationException(
            "Embedded BirdsOfAFeather detected inside LetMeHatchThisEgg. " +
            "Remove those files/folders and install BirdsOfAFeather separately."
        );
    }

    private static List<string> FindLocalBirdsOfAFeatherCopies(string modDirectoryPath)
    {
        static bool IsIgnoredPath(string fullPath)
        {
            var normalized = fullPath.Replace('\\', '/');
            return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/.git/", StringComparison.OrdinalIgnoreCase);
        }

        var offenders = new List<string>();

        if (!Directory.Exists(modDirectoryPath))
        {
            return offenders;
        }

        foreach (var dir in Directory.EnumerateDirectories(modDirectoryPath, "*", SearchOption.AllDirectories))
        {
            if (IsIgnoredPath(dir))
            {
                continue;
            }

            var name = Path.GetFileName(dir);
            if (string.Equals(name, "BirdsOfAFeather", StringComparison.OrdinalIgnoreCase))
            {
                offenders.Add(dir);
            }
        }

        foreach (var file in Directory.EnumerateFiles(modDirectoryPath, "*.dll", SearchOption.AllDirectories))
        {
            if (IsIgnoredPath(file))
            {
                continue;
            }

            var name = Path.GetFileName(file);
            if (name.Contains("BirdsOfAFeather", StringComparison.OrdinalIgnoreCase))
            {
                offenders.Add(file);
            }
        }

        return offenders.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
    }
}
