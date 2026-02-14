namespace WildEggsHatching.Application.Spawning;

public interface IGameStateQueryEvaluator
{
    bool Check(string expression);
}
