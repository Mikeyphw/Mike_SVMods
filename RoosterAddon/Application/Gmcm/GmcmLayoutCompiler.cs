using System;
using System.Collections.Generic;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;

namespace RoosterAddon.Application.Gmcm;

internal static class GmcmLayoutCompiler
{
    public static void Apply<TConfig>(
        GmcmPageBuilder<TConfig> builder,
        IReadOnlyList<IGmcmDescriptor<TConfig>> descriptors)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (descriptors is null) throw new ArgumentNullException(nameof(descriptors));

        foreach (var d in descriptors)
            d.Apply(builder);
    }
}
