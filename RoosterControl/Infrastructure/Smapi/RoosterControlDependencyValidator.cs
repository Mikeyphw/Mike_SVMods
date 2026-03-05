using System;
using System.Collections.Generic;
using StardewModdingAPI;

namespace RoosterControl.Infrastructure.Smapi;

/// <summary>Validates that required and optional dependencies are installed.</summary>
internal static class RoosterControlDependencyValidator
{
    private const string NestCoreModId = "Mikew.nestcore";
    private const string RoostersModId = "Meara.Roosters";
    private static readonly SemanticVersion MinNestCoreVersion = new(1, 0, 1);

    /// <summary>Validates dependencies and throws if required ones are missing or out of date.</summary>
    /// <param name="helper">The SMAPI mod helper.</param>
    /// <param name="monitor">The monitor for logging warnings/errors.</param>
    /// <exception cref="InvalidOperationException">Thrown if required dependencies are missing or out of date.</exception>
    internal static void ValidateOrThrow(IModHelper helper, IMonitor monitor)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Check required NestCore dependency
        var nestCore = helper.ModRegistry.Get(NestCoreModId);
        if (nestCore is null)
        {
            errors.Add($"Missing required dependency: {NestCoreModId} (NestCore). Install it and ensure SMAPI can load it.");
        }
        else
        {
            if (nestCore.Manifest.Version.IsOlderThan(MinNestCoreVersion))
                errors.Add($"NestCore is out of date: found {nestCore.Manifest.Version}, required >= {MinNestCoreVersion}.");
        }

        // Check optional Roosters integration
        if (!helper.ModRegistry.IsLoaded(RoostersModId))
            warnings.Add($"Optional integration not installed: {RoostersModId}. Rooster providers from that mod will be unavailable.");

        // Log warnings
        foreach (var w in warnings)
            monitor.Log(w, LogLevel.Warn);

        // Throw if there are errors
        if (errors.Count == 0)
            return;

        monitor.Log("RoosterControl cannot start due to dependency issues:", LogLevel.Error);
        foreach (var e in errors)
            monitor.Log($"- {e}", LogLevel.Error);

        throw new InvalidOperationException("Missing required dependencies. See log for details.");
    }
}

