using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI;
using Mikew.NestCore.Helpers.Logging;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace LetMeHatch.Infrastructure.Smapi;

public static class DependencyGuards
{
    public const string NestCoreId = "Mikew.nestcore";

    public static void AssertNestCorePresent(IModRegistry registry)
    {
        if (!registry.IsLoaded(NestCoreId))
        {
            throw new InvalidOperationException(
                $"LetMeHatch requires NestCore ({NestCoreId}). " +
                "Install/enable it and try again."
            );
        }
    }

    public static void AssertNoLocalNestCoreCopies(string modDirectoryPath, ILog log)
    {
        var offenders = FindLocalNestCoreCopies(modDirectoryPath);

        if (offenders.Count == 0)
        {
            return;
        }

        LogScopedHelper.Log(log, ScopedLogCatalog.ModLthteCategory.Integrations,
            "Found local NestCore copies inside LetMeHatch. " +
            "NestCore must be installed as its own mod; remove embedded copies.",
            LogLevel.Error
        );

        foreach (var offender in offenders)
        {
            LogScopedHelper.Log(log, ScopedLogCatalog.ModLthteCategory.Integrations, $"  - {offender}", LogLevel.Error);
        }

        throw new InvalidOperationException(
            "Embedded NestCore detected inside LetMeHatch. " +
            "Remove those files/folders and install NestCore separately."
        );
    }

    private static List<string> FindLocalNestCoreCopies(string modDirectoryPath)
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
            if (string.Equals(name, "NestCore", StringComparison.OrdinalIgnoreCase))
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
            if (name.Contains("NestCore", StringComparison.OrdinalIgnoreCase))
            {
                offenders.Add(file);
            }
        }

        return offenders.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
    }
}
