using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace RoosterControl.Framework.Config;

public sealed partial class GeneralSettings
{
    /// <summary>
    /// Keybind to open the StardewUI editor hub focused on White rooster normal produce.
    /// </summary>
    public KeybindList OpenWhiteRoosterProduceEditorKey { get; set; } = new(SButton.F7);

    /// <summary>
    /// Optional shortcut to open the duplicate StardewUI compat editor directly.
    /// </summary>
    public KeybindList OpenCompatEditorKey { get; set; } = new();
}
