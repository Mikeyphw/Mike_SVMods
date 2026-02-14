namespace WildEggsHatching.Core.Enums;

/// <summary>
/// Enum representing all possible animal keys used throughout the mod.
/// Replaces string keys like "VoidChickenKey" to prevent typos and enable compile-time checking.
/// </summary>
public enum AnimalKeyType
{
    // === Standard Animals ===
    
    /// <summary>Chicken - the starter animal.</summary>
    ChickenKey,
    
    /// <summary>Duck - unlocks after first Special Order.</summary>
    DuckKey,
    
    /// <summary>Dinosaur - unlocks after Wizard quest.</summary>
    DinosaurKey,
    
    // === Magic Animals ===
    
    /// <summary>Void Chicken - rare magic variant.</summary>
    VoidChickenKey,
    
    /// <summary>Phoenix - fire bird (if implemented).</summary>
    PhoenixKey,
    
    /// <summary>Crystal Bird - crystalline creature.</summary>
    CrystalBirdKey,
    
    /// <summary>Shadow Creature - void variant.</summary>
    ShadowCreatureKey,
    
    // === Large Egg Animals ===
    
    /// <summary>Ostrich - large bird.</summary>
    OstrichKey,
    
    /// <summary>Emu - large Australian bird.</summary>
    EmuKey,
    
    /// <summary>Turkey - seasonal bird.</summary>
    TurkeyKey,
    
    /// <summary>Peacock - decorative bird.</summary>
    PeacockKey,
    
    // === Duck Variants ===
    
    /// <summary>Muscovy Duck.</summary>
    MuscovyDuckKey,
    
    /// <summary>Rouen Duck.</summary>
    RouenDuckKey,
    
    /// <summary>Mandarin Duck.</summary>
    MandarinDuckKey,
    
    // === Special/Event Animals ===
    
    /// <summary>Golden Chicken - rare variant.</summary>
    GoldenChickenKey,
    
    /// <summary>Prismatic Bird - prismatic color variant.</summary>
    PrismaticBirdKey,
    
    /// <summary>Dodo - flightless bird.</summary>
    DodoKey,
    
    /// <summary>Kiwi - small flightless bird.</summary>
    KiwiKey,
    
    /// <summary>Puffin - seabird.</summary>
    PuffinKey,
    
    /// <summary>Loon - water bird.</summary>
    LoonKey,
    
    /// <summary>Penguin - cold-weather bird.</summary>
    PenguinKey,
    
    // === Fantasy/Mythic ===
    
    /// <summary>Griffin - mythical creature.</summary>
    GriffinKey,
    
    /// <summary>Hippogriff - mythical creature.</summary>
    HippogriffKey,
    
    /// <summary>Wyvern - dragon variant.</summary>
    WyvernKey,
    
    /// <summary>Dragon - full dragon.</summary>
    DragonKey,
    
    /// <summary>Phoenix (Alternative) - fire elemental.</summary>
    PhoenixAltKey,
    
    /// <summary>Baba Yaga's chicken.</summary>
    BabaYagaKey,
    
    // === Hybrid/Unknown ===
    
    /// <summary>Jekks Dino - mod-specific.</summary>
    JekksDinoKey,
    
    /// <summary>Livipup Dino - mod-specific.</summary>
    LivipupDinoKey,
    
    /// <summary>PepperRex - mod-specific dinosaur.</summary>
    PepperRexKey,
    
    /// <summary>Lovebird variants.</summary>
    LovebirdAKey,
    LovebirdBKey,
    LovebirdABKey,
    
    /// <summary>Quail variants.</summary>
    QuailAKey,
    QuailBKey,
    QuailABCKey,
    
    /// <summary>Owl variants.</summary>
    OwlAKey,
    OwlBKey,
    OwlABKey,
    
    /// <summary>Parrot variants (Peacock family).</summary>
    ParrotKey,
    
    // === Other ===
    
    /// <summary>Canary - small songbird.</summary>
    CanaryKey,
    
    /// <summary>Pigeon - common bird.</summary>
    PigeonKey,
    
    /// <summary>Crow - common bird.</summary>
    CrowKey,
    
    /// <summary>Wild Parrot.</summary>
    WildParrotKey,
    
    /// <summary>Turtle - not a bird but uses egg spawning.</summary>
    TurtleKey,
    
    /// <summary>Snorlax-like creature.</summary>
    SnorlaxKey,
    
    /// <summary>Psyduck variant.</summary>
    PsyduckKey,
    
    /// <summary>Battery Robot - mechanical creature.</summary>
    BatteryRobotKey,
    
    /// <summary>Munchlax - small creature.</summary>
    MunchlaxKey,
    
    /// <summary>Slowpoke variant.</summary>
    SlowpokeKey,
    
    /// <summary>Slowbro variant.</summary>
    SlowbroKey,
    
    /// <summary>Slowking variant.</summary>
    SlowkingKey,
}

/// <summary>
/// Extension methods for AnimalKeyType enum.
/// </summary>
public static class AnimalKeyTypeExtensions
{
    /// <summary>
    /// Converts an AnimalKeyType to its string representation.
    /// </summary>
    public static string ToKeyString(this AnimalKeyType key)
    {
        return key switch
        {
            AnimalKeyType.ChickenKey => "ChickenKey",
            AnimalKeyType.DuckKey => "DuckKey",
            AnimalKeyType.DinosaurKey => "DinosaurKey",
            AnimalKeyType.VoidChickenKey => "VoidChickenKey",
            AnimalKeyType.PhoenixKey => "PhoenixKey",
            AnimalKeyType.CrystalBirdKey => "CrystalBirdKey",
            AnimalKeyType.ShadowCreatureKey => "ShadowCreatureKey",
            AnimalKeyType.OstrichKey => "OstrichKey",
            AnimalKeyType.EmuKey => "EmuKey",
            AnimalKeyType.TurkeyKey => "TurkeyKey",
            AnimalKeyType.PeacockKey => "PeacockKey",
            AnimalKeyType.MuscovyDuckKey => "MuscovyDuckKey",
            AnimalKeyType.RouenDuckKey => "RouenDuckKey",
            AnimalKeyType.MandarinDuckKey => "MandarinDuckKey",
            AnimalKeyType.GoldenChickenKey => "GoldenChickenKey",
            AnimalKeyType.PrismaticBirdKey => "PrismaticBirdKey",
            AnimalKeyType.DodoKey => "DodoKey",
            AnimalKeyType.KiwiKey => "KiwiKey",
            AnimalKeyType.PuffinKey => "PuffinKey",
            AnimalKeyType.LoonKey => "LoonKey",
            AnimalKeyType.PenguinKey => "PenguinKey",
            AnimalKeyType.GriffinKey => "GriffinKey",
            AnimalKeyType.HippogriffKey => "HippogriffKey",
            AnimalKeyType.WyvernKey => "WyvernKey",
            AnimalKeyType.DragonKey => "DragonKey",
            AnimalKeyType.PhoenixAltKey => "PhoenixAltKey",
            AnimalKeyType.BabaYagaKey => "BabaYagaKey",
            AnimalKeyType.JekksDinoKey => "JekksDinoKey",
            AnimalKeyType.LivipupDinoKey => "LivipupDinoKey",
            AnimalKeyType.PepperRexKey => "PepperRexKey",
            AnimalKeyType.LovebirdAKey => "LovebirdAKey",
            AnimalKeyType.LovebirdBKey => "LovebirdBKey",
            AnimalKeyType.LovebirdABKey => "LovebirdABKey",
            AnimalKeyType.QuailAKey => "QuailAKey",
            AnimalKeyType.QuailBKey => "QuailBKey",
            AnimalKeyType.QuailABCKey => "QuailABCKey",
            AnimalKeyType.OwlAKey => "OwlAKey",
            AnimalKeyType.OwlBKey => "OwlBKey",
            AnimalKeyType.OwlABKey => "OwlABKey",
            AnimalKeyType.ParrotKey => "ParrotKey",
            AnimalKeyType.CanaryKey => "CanaryKey",
            AnimalKeyType.PigeonKey => "PigeonKey",
            AnimalKeyType.CrowKey => "CrowKey",
            AnimalKeyType.WildParrotKey => "WildParrotKey",
            AnimalKeyType.TurtleKey => "TurtleKey",
            AnimalKeyType.SnorlaxKey => "SnorlaxKey",
            AnimalKeyType.PsyduckKey => "PsyduckKey",
            AnimalKeyType.BatteryRobotKey => "BatteryRobotKey",
            AnimalKeyType.MunchlaxKey => "MunchlaxKey",
            AnimalKeyType.SlowpokeKey => "SlowpokeKey",
            AnimalKeyType.SlowbroKey => "SlowbroKey",
            AnimalKeyType.SlowkingKey => "SlowkingKey",
            _ => key.ToString()
        };
    }

    /// <summary>
    /// Gets the animal display name for a key.
    /// </summary>
    public static string ToDisplayName(this AnimalKeyType key)
    {
        return key switch
        {
            AnimalKeyType.ChickenKey => "Chicken",
            AnimalKeyType.DuckKey => "Duck",
            AnimalKeyType.DinosaurKey => "Dinosaur",
            AnimalKeyType.VoidChickenKey => "Void Chicken",
            AnimalKeyType.PhoenixKey => "Phoenix",
            AnimalKeyType.CrystalBirdKey => "Crystal Bird",
            AnimalKeyType.ShadowCreatureKey => "Shadow Creature",
            AnimalKeyType.OstrichKey => "Ostrich",
            AnimalKeyType.EmuKey => "Emu",
            AnimalKeyType.TurkeyKey => "Turkey",
            AnimalKeyType.PeacockKey => "Peacock",
            AnimalKeyType.MuscovyDuckKey => "Muscovy Duck",
            AnimalKeyType.RouenDuckKey => "Rouen Duck",
            AnimalKeyType.MandarinDuckKey => "Mandarin Duck",
            AnimalKeyType.GoldenChickenKey => "Golden Chicken",
            AnimalKeyType.PrismaticBirdKey => "Prismatic Bird",
            AnimalKeyType.DodoKey => "Dodo",
            AnimalKeyType.KiwiKey => "Kiwi",
            AnimalKeyType.PuffinKey => "Puffin",
            AnimalKeyType.LoonKey => "Loon",
            AnimalKeyType.PenguinKey => "Penguin",
            AnimalKeyType.GriffinKey => "Griffin",
            AnimalKeyType.HippogriffKey => "Hippogriff",
            AnimalKeyType.WyvernKey => "Wyvern",
            AnimalKeyType.DragonKey => "Dragon",
            AnimalKeyType.PhoenixAltKey => "Phoenix",
            AnimalKeyType.BabaYagaKey => "Baba Yaga's Chicken",
            AnimalKeyType.JekksDinoKey => "Jekks Dino",
            AnimalKeyType.LivipupDinoKey => "Livipup Dino",
            AnimalKeyType.PepperRexKey => "PepperRex",
            AnimalKeyType.LovebirdAKey => "Lovebird A",
            AnimalKeyType.LovebirdBKey => "Lovebird B",
            AnimalKeyType.LovebirdABKey => "Lovebird AB",
            AnimalKeyType.QuailAKey => "Quail A",
            AnimalKeyType.QuailBKey => "Quail B",
            AnimalKeyType.QuailABCKey => "Quail ABC",
            AnimalKeyType.OwlAKey => "Owl A",
            AnimalKeyType.OwlBKey => "Owl B",
            AnimalKeyType.OwlABKey => "Owl AB",
            AnimalKeyType.ParrotKey => "Parrot",
            AnimalKeyType.CanaryKey => "Canary",
            AnimalKeyType.PigeonKey => "Pigeon",
            AnimalKeyType.CrowKey => "Crow",
            AnimalKeyType.WildParrotKey => "Wild Parrot",
            AnimalKeyType.TurtleKey => "Turtle",
            AnimalKeyType.SnorlaxKey => "Snorlax",
            AnimalKeyType.PsyduckKey => "Psyduck",
            AnimalKeyType.BatteryRobotKey => "Battery Robot",
            AnimalKeyType.MunchlaxKey => "Munchlax",
            AnimalKeyType.SlowpokeKey => "Slowpoke",
            AnimalKeyType.SlowbroKey => "Slowbro",
            AnimalKeyType.SlowkingKey => "Slowking",
            _ => key.ToString()
        };
    }

    /// <summary>
    /// Tries to parse a string into an AnimalKeyType.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="result">The parsed enum value.</param>
    /// <returns>True if parsing succeeded.</returns>
    public static bool TryParse(string value, out AnimalKeyType result)
    {
        // Try direct enum parse first
        if (Enum.TryParse<AnimalKeyType>(value, true, out result))
        {
            return true;
        }

        // Try with "Key" suffix removed
        if (value.EndsWith("Key", StringComparison.OrdinalIgnoreCase))
        {
            var baseName = value.Substring(0, value.Length - 3);
            if (Enum.TryParse<AnimalKeyType>(baseName + "Key", true, out result))
            {
                return true;
            }
        }

        // Try mapping common names
        result = value switch
        {
            "Chicken" => AnimalKeyType.ChickenKey,
            "Duck" => AnimalKeyType.DuckKey,
            "Dinosaur" => AnimalKeyType.DinosaurKey,
            "Void Chicken" or "VoidChicken" => AnimalKeyType.VoidChickenKey,
            _ => AnimalKeyType.ChickenKey // Default fallback
        };

        return value is "Chicken" or "Duck" or "Dinosaur" or "Void Chicken" or "VoidChicken";
    }
}

