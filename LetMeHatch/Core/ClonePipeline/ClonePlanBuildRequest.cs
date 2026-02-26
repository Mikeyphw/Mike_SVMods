using System.Collections.Generic;

namespace LetMeHatch.Core.ClonePipeline;

/// <summary>
/// Inputs required to compute a deterministic clone plan.
/// </summary>
public sealed record ClonePlanBuildRequest(
    string ModUniqueId,
    IReadOnlyDictionary<string, FarmAnimalProduceLists> Animals
);
