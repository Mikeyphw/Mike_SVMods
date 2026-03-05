using System.Collections.Generic;
using System.Linq;

namespace EggsInWild.Infrastructure.Diagnostics;

public class WeggVerifyReport
{
    public List<WeggVerifyCheck> Checks { get; } = new();

    public int PassCount => Checks.Count(x => x.Status == WeggVerifyStatus.Pass);
    public int WarnCount => Checks.Count(x => x.Status == WeggVerifyStatus.Warn);
    public int FailCount => Checks.Count(x => x.Status == WeggVerifyStatus.Fail);

    public bool Success => FailCount == 0;

    public void Add(string id, WeggVerifyStatus status, string message) => Checks.Add(new WeggVerifyCheck(id, status, message));
}

