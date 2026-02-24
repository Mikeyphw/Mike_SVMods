namespace LetMeHatch.Infrastructure.Content;

/// <summary>Optional debug metadata for content loaders/editors.</summary>
public interface IContentHandlerDebugInfo
{
    /// <summary>What this handler targets (asset name/pattern).</summary>
    string DebugHandles { get; }

    /// <summary>High-level gating conditions (config/state) that commonly short-circuit execution.</summary>
    string DebugGates { get; }
}
