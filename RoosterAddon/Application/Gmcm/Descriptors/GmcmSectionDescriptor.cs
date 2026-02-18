using System;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;

namespace RoosterAddon.Application.Gmcm.Descriptors;

internal sealed record GmcmSectionDescriptor<TConfig>(string SectionId, bool Tooltip = false) : IGmcmDescriptor<TConfig>
{
    public GmcmDescriptorKind Kind => GmcmDescriptorKind.Section;
    public string Id => SectionId;

    public void Apply(GmcmPageBuilder<TConfig> builder)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        builder.Section(SectionId, tooltip: Tooltip);
    }
}
