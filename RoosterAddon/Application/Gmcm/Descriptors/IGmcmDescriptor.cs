using Mikew.BirdsOfAFeather.Integrations.Gmcm;

namespace RoosterAddon.Application.Gmcm.Descriptors;

internal interface IGmcmDescriptor<TConfig>
{
    GmcmDescriptorKind Kind { get; }
    string Id { get; }
    void Apply(GmcmPageBuilder<TConfig> builder);
}
