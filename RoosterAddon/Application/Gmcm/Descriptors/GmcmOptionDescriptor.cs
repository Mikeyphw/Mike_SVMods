using System;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;

namespace RoosterAddon.Application.Gmcm.Descriptors;

internal sealed record GmcmOptionDescriptor<TConfig>(string OptionId, Action<GmcmPageBuilder<TConfig>> Add, bool Tooltip = true) : IGmcmDescriptor<TConfig>
{
    public GmcmDescriptorKind Kind => GmcmDescriptorKind.Option;
    public string Id => OptionId;

    public void Apply(GmcmPageBuilder<TConfig> builder)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        Add(builder);
    }
}
