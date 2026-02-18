using System;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;

namespace RoosterAddon.Application.Gmcm.Descriptors;

internal sealed record GmcmPageLinkDescriptor<TConfig>(string PageId, bool Tooltip = false) : IGmcmDescriptor<TConfig>
{
    public GmcmDescriptorKind Kind => GmcmDescriptorKind.PageLink;
    public string Id => PageId;

    public void Apply(GmcmPageBuilder<TConfig> builder)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        builder.Link(PageId, tooltip: Tooltip);
    }
}
