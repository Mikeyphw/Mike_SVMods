using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace RoosterAddon.Integrations;

/// <summary>Minimal Content Patcher API surface used by this mod.</summary>
public interface IContentPatcherAPI
{
    object ParseConditions(IManifest manifest, IDictionary<string, string?> rawConditions, SemanticVersion? formatVersion = null);
}
