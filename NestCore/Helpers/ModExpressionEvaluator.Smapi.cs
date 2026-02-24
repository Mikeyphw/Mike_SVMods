using StardewModdingAPI;

namespace Mikew.NestCore.Helpers;

public static partial class ModIdEvaluator
{
    public static bool Evaluate(string expression, IModRegistry modRegistry)
    {
        return Evaluate(expression, modRegistry.IsLoaded);
    }
}
