using System;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;

namespace RoosterAddon.Application.Gmcm.Descriptors;

internal sealed record GmcmParagraphDescriptor<TConfig>(string ParagraphId) : IGmcmDescriptor<TConfig>
{
    public GmcmDescriptorKind Kind => GmcmDescriptorKind.Paragraph;
    public string Id => ParagraphId;

    public void Apply(GmcmPageBuilder<TConfig> builder)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        builder.Paragraph(ParagraphId);
    }
}
