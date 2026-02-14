using WildEggsHatching.Infrastructure.ModData.Models;

namespace WildEggsHatching.Application.Spawning;

public interface IAreaConditionsEvaluator
{
    bool AreMet(ExtraConditionsJson conditions);
}
