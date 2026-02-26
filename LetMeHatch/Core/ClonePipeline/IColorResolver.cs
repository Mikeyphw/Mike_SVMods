using Microsoft.Xna.Framework;

namespace LetMeHatch.Core.ClonePipeline;

/// <summary>
/// Resolves an item color deterministically without mutating config/save state.
/// </summary>
public interface IColorResolver
{
    Color Resolve(ColorResolutionContext context);
}
