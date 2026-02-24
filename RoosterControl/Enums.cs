using System;

namespace RoosterControl.Enums
{
    public enum RoosterItemDataEntries
    {
        Name,
        Probability
    }

    public enum EggTypeEntries
    {
        Normal,
        Large,
        Fertilized
    }

    public enum ProduceEntries
    {
        First,
        Second,
        Third,
        Fourth,
        Chosen
    }

    public enum ProduceTypeEntries
    {
        Produce,
        Deluxe
    }

    public enum RoosterType
    {
        White,
        Brown,
        Blue,
        Gold,
        Void,
        LyellBrownRooster,
        LyellWhiteRooster,
        LyellBlueRooster,
        LyellTransRooster,
    }

    public enum Textures
    {
        WhiteRooster,
        BrownRooster,
        BlueRooster,
        GoldRooster,
        VoidRooster,
        AllRoosters,
        VoidShopIcon,
        GoldShopIcon
    }

    public enum TextureDetails
    {
        GamePath,
        MoldFolderPath
    }

    [Obsolete("Use Mikew.NestCore.EnumsAndStrings.Enums.LuckyTypes instead.")]
    public enum LuckyTypes
    {
        SuperUnlucky,
        Unlucky,
        Neutral,
        Lucky,
        SuperLucky
    }
}
