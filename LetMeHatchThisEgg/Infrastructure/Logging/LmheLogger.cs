using Mikew.BirdsOfAFeather.Framework;
using StardewModdingAPI;
using LetMeHatchThisEgg.Core.Abstractions;

namespace LetMeHatchThisEgg.Infrastructure.Logging;

/// <summary>
/// LMH logging wrapper which delegates to BirdsOfAFeather's logger while providing lightweight categorization.
/// </summary>
public sealed class LmheLogger : ILogger
{
    private readonly Logger boafLogger;
    private readonly string prefix;

    /// <summary>Creates a new LMH logger wrapper.</summary>
    /// <param name="boafLogger">The underlying BirdsOfAFeather logger.</param>
    /// <param name="prefix">Optional prefix/category prepended to messages.</param>
    public LmheLogger(Logger boafLogger, string? prefix = null)
    {
        this.boafLogger = boafLogger ?? throw new ArgumentNullException(nameof(boafLogger));
        this.prefix = string.IsNullOrWhiteSpace(prefix) ? string.Empty : prefix.Trim();
    }

    /// <inheritdoc />
    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        if (exception is not null)
            message = $"{message}{Environment.NewLine}{exception}";

        boafLogger.VerboseLog(Format(message), level);
    }

    /// <inheritdoc />
    public ILogger ForCategory(string category)
    {
        var next = string.IsNullOrWhiteSpace(prefix)
            ? category
            : $"{prefix}/{category}";
        return new LmheLogger(boafLogger, next);
    }

    private string Format(string message)
        => string.IsNullOrWhiteSpace(prefix) ? message : $"[{prefix}] {message}";
}
