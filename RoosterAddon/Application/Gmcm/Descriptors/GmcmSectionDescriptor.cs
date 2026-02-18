using Mikew.BirdsOfAFeather.Integrations.Gmcm;

namespace RoosterAddon.Application.Gmcm.Descriptors;

/// <summary>GMCM section header, translated via <c>config.section.&lt;id&gt;.text</c>.</summary>
internal sealed record GmcmSectionDescriptor<TConfig>(string SectionId, bool Tooltip = false) : IGmcmElementDescriptor<TConfig>
{
    public void Apply(GmcmPageBuilder<TConfig> builder)
        => builder.Section(SectionId, tooltip: Tooltip);
}
