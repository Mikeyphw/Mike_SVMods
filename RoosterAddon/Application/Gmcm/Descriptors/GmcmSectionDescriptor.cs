using System.Collections.Generic;

namespace RoosterAddon.Application.Gmcm.Descriptors;

/// <summary>Descriptor for a titled section with entries.</summary>
internal sealed record GmcmSectionDescriptor(
    string SectionId,
    bool Tooltip,
    IReadOnlyList<IGmcmEntryDescriptor> Entries);
