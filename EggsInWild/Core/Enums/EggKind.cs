namespace EggsInWild.Core.Enums;

public enum EggKind
{
    Wild = 0,
    LargeWild = 1,
    Magic = 2,
    LargeMagic = 3,
}

public static class EggKindExtensions
{
    public static bool IsLarge(this EggKind kind)
        => kind is EggKind.LargeWild or EggKind.LargeMagic;

    public static bool IsMagic(this EggKind kind)
        => kind is EggKind.Magic or EggKind.LargeMagic;

    public static string ToSpawnParametersKey(this EggKind kind) => kind switch
    {
        EggKind.Wild => "wildEgg",
        EggKind.LargeWild => "largeWildEgg",
        EggKind.Magic => "magicWildEgg",
        EggKind.LargeMagic => "largeMagicWildEgg",
        _ => "wildEgg",
    };

    public static EggKind FromEggItemId(string eggItemId)
    {
        // Expected IDs like: Mikew.eggsinwild_ForestWEgg, _ForestLWEgg, _ForestMWEgg, _ForestLMWEgg
        var isMagic = eggItemId.Contains("MWEgg");
        var isLarge = eggItemId.Contains("LWEgg") || eggItemId.Contains("LMWEgg");

        return (isMagic, isLarge) switch
        {
            (false, false) => EggKind.Wild,
            (false, true) => EggKind.LargeWild,
            (true, false) => EggKind.Magic,
            (true, true) => EggKind.LargeMagic,
        };
    }
}
