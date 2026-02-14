using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace Mikew.BirdsOfAFeather.Framework;

/// <summary>
/// Minimal Content Patcher API surface used by these mods.
/// Avoids compile-time dependency on Content Patcher's assemblies.
/// </summary>
public interface IContentPatcherAPI
{
    void RegisterToken(IManifest mod, string name, object token);

    object ParseConditions(
        IManifest manifest,
        IDictionary<string, string?>? rawConditions,
        ISemanticVersion formatVersion,
        string[]? assumeModIds = null
    );

    object ParseTokenString(
        IManifest manifest,
        string rawValue,
        ISemanticVersion formatVersion,
        string[]? assumeModIds = null
    );
}
