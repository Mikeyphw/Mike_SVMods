using StardewValley;
using WildEggsHatching.Application.Spawning;

namespace WildEggsHatching.Infrastructure.Game;

public sealed class SmapiGameStateQueryEvaluator : IGameStateQueryEvaluator
{
    public bool Check(string expression) => GameStateQuery.CheckConditions(expression);
}
