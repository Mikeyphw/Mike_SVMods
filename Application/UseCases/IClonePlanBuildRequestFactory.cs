using LetMeHatch.Core.ClonePipeline;

namespace LetMeHatch.Application.UseCases;

/// <summary>
/// Creates a Core <see cref="ClonePlanBuildRequest"/> from game data.
/// </summary>
public interface IClonePlanBuildRequestFactory
{
    ClonePlanBuildRequest Create(string modUniqueId);
}
