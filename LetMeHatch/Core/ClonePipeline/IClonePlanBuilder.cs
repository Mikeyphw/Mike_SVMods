namespace LetMeHatch.Core.ClonePipeline;

/// <summary>
/// Builds a deterministic clone plan from animal produce snapshots.
/// </summary>
public interface IClonePlanBuilder
{
    ClonePlan Build(ClonePlanBuildRequest request);
}
