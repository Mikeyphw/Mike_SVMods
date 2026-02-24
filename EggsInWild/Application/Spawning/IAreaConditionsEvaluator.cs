using EggsInWild.Infrastructure.ModData.Models;

namespace EggsInWild.Application.Spawning;

public interface IAreaConditionsEvaluator
{
    bool AreMet(ExtraConditionsJson conditions);
}
