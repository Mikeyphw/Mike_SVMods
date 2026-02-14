using System;
using System.Collections.Generic;
using ContentPatcher.Framework.Tokens.ValueProviders.ModConvention;
using StardewModdingAPI;
using ContentPatcher;
using StardewModdingAPI.Events;

namespace RoosterAddon.Framework
{
    public interface IContentPatcherAPI
    {
            	void RegisterToken(IManifest mod, string name, object token);
    			        IManagedConditions ParseConditions(IManifest manifest, IDictionary<string, string?>? rawConditions, ISemanticVersion formatVersion, string[]? assumeModIds = null);
    }
}
