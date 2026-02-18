using System.Collections.Generic;

namespace RoosterAddon.Application.Gmcm.Descriptors;

internal sealed record GmcmPageDescriptor<TConfig>(
    string PageId,
    IReadOnlyList<IGmcmDescriptor<TConfig>> Content);
