// FILE: NestCore/Integrations/StardewUI/Dsl/StardewUiLayout.cs
// DSL for defining StardewUI menus and HUD drawables.

using System;
using System.Collections.Generic;

namespace Mikew.NestCore.Integrations.StardewUI.Dsl;

/// <summary>
/// Defines a StardewUI layout with menu and HUD definitions.
/// </summary>
public sealed class StardewUiLayout
{
    private readonly Dictionary<string, Action<StardewUiMenuBuilder>> _menuDefinitions = new();
    private readonly Dictionary<string, Action<StardewUiHudBuilder>> _hudDefinitions = new();

    /// <summary>
    /// Defines a menu with the given ID.
    /// </summary>
    /// <param name="menuId">Unique identifier for the menu.</param>
    /// <param name="define">Builder action to configure the menu.</param>
    public void Menu(string menuId, Action<StardewUiMenuBuilder> define)
    {
        if (string.IsNullOrWhiteSpace(menuId))
            throw new ArgumentException("Menu ID cannot be null or empty.", nameof(menuId));
        
        _menuDefinitions[menuId] = define ?? throw new ArgumentNullException(nameof(define));
    }

    /// <summary>
    /// Defines a HUD drawable with the given ID.
    /// </summary>
    /// <param name="hudId">Unique identifier for the HUD.</param>
    /// <param name="define">Builder action to configure the HUD.</param>
    public void Hud(string hudId, Action<StardewUiHudBuilder> define)
    {
        if (string.IsNullOrWhiteSpace(hudId))
            throw new ArgumentException("HUD ID cannot be null or empty.", nameof(hudId));
        
        _hudDefinitions[hudId] = define ?? throw new ArgumentNullException(nameof(define));
    }

    /// <summary>
    /// Tries to get a menu definition by ID.
    /// </summary>
    internal bool TryGetMenu(string menuId, out Action<StardewUiMenuBuilder>? builder)
        => _menuDefinitions.TryGetValue(menuId, out builder);

    /// <summary>
    /// Tries to get a HUD definition by ID.
    /// </summary>
    internal bool TryGetHud(string hudId, out Action<StardewUiHudBuilder>? builder)
        => _hudDefinitions.TryGetValue(hudId, out builder);
}

