using System.Collections.Generic;

namespace Mikew.NestCore.Framework;

/// <summary>
/// Public API exposed by RoosterControl for optional compat consumers like LetMeHatch.
/// </summary>
public interface IRoosterControlApi
{
    int ApiVersion { get; }

    IReadOnlyDictionary<string, string> GetResolvedEggItemIds();
}
