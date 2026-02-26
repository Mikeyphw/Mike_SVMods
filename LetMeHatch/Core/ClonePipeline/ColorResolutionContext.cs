using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LetMeHatch.Core.ClonePipeline;

/// <summary>
/// Inputs for deterministic color resolution.
/// </summary>
public sealed record ColorResolutionContext(
    string Key,
    string SeedKey,
    IReadOnlyDictionary<string, Color>? ConfigOverrides = null,
    IReadOnlyDictionary<string, Color>? SavedOverrides = null,
    IReadOnlyCollection<string>? ContextTags = null
);
