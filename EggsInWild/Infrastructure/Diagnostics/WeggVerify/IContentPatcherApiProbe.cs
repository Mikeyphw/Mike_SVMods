using System.Collections.Generic;

namespace EggsInWild.Infrastructure.Diagnostics;

public interface IContentPatcherApiProbe
{
    IEnumerable<object>? GetPatches();
}

