using System.Collections.Generic;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;
using RoosterAddon.Application.Gmcm.Descriptors;

namespace RoosterAddon.Application.Gmcm;

/// <summary>Compiles descriptor elements into BOAF's GMCM layout builder.</summary>
internal static class GmcmLayoutCompiler
{
    public static void Apply<TConfig>(GmcmPageBuilder<TConfig> builder, IEnumerable<IGmcmElementDescriptor<TConfig>> elements)
    {
        foreach (var e in elements)
            e.Apply(builder);
    }
}
