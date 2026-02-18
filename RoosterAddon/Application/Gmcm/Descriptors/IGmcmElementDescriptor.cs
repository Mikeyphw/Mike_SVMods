using Mikew.BirdsOfAFeather.Integrations.Gmcm;

namespace RoosterAddon.Application.Gmcm.Descriptors;

/// <summary>Descriptor element that can render itself into a BOAF GMCM page builder.</summary>
internal interface IGmcmElementDescriptor<TConfig>
{
    void Apply(GmcmPageBuilder<TConfig> builder);
}
