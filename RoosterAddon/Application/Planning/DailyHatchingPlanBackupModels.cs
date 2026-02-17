using System;

namespace RoosterAddon.Application.Planning;

/// <summary>Persisted backup of a daily plan when corruption is detected.</summary>
internal sealed class DailyHatchingPlanCorruptBackup
{
    public int Version { get; set; } = 1;

    /// <summary>UTC unix milliseconds.</summary>
    public long TimestampUtcUnixMs { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DailyHatchingPlanEnvelope? Plan { get; set; }
}
