using System;
using System.Collections.Generic;
using System.Linq;
using RoosterAddon.Application.Gmcm.Descriptors;
using RoosterAddon.Framework.Config;

namespace RoosterAddon.Application.Gmcm;

internal sealed record RoosterGmcmDescriptorIndex(
    IReadOnlyList<string> OptionIds,
    IReadOnlyList<string> SectionIds,
    IReadOnlyList<string> PageIds)
{
    public static RoosterGmcmDescriptorIndex Build()
    {
        var optionIds = new List<string>();
        var sectionIds = new List<string>();
        var pageIds = new List<string>();

        void Scan(IEnumerable<IGmcmDescriptor<RoosterAddonConfig>> ds)
        {
            foreach (var d in ds)
            {
                switch (d.Kind)
                {
                    case GmcmDescriptorKind.Option:
                        optionIds.Add(d.Id);
                        break;
                    case GmcmDescriptorKind.Section:
                        sectionIds.Add(d.Id);
                        break;
                }
            }
        }

        Scan(Descriptors.RoosterGmcmDescriptorRegistry.RootGeneral());
        Scan(Descriptors.RoosterGmcmDescriptorRegistry.RootRoostersLinks());

        foreach (var page in Descriptors.RoosterGmcmDescriptorRegistry.Pages(() => RoosterAddonConfig.CreateDefault()))
        {
            pageIds.Add(page.PageId);
            Scan(page.Content);
        }

        return new RoosterGmcmDescriptorIndex(
            OptionIds: optionIds,
            SectionIds: sectionIds,
            PageIds: pageIds);
    }

    public IReadOnlyList<string> DuplicateOptionIds()
        => OptionIds.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
}
