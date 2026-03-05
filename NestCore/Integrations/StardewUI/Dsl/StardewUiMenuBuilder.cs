// FILE: NestCore/Integrations/StardewUI/Dsl/StardewUiMenuBuilder.cs
// DSL builder for StardewUI menus.

using System;
using StardewUI.Framework;

namespace Mikew.NestCore.Integrations.StardewUI.Dsl;

/// <summary>
/// Builder for configuring a StardewUI menu.
/// </summary>
public sealed class StardewUiMenuBuilder
{
    private string? _viewName;
    private Func<object?>? _contextProvider;
    private Action<IMenuController>? _controllerConfig;
    private Action? _onClosing;
    private Action? _onClosed;

    /// <summary>
    /// Sets the view name for this menu. Required - throws if not set.
    /// </summary>
    /// <param name="viewName">The view name (short name or qualified asset name).</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if view name is not set.</exception>
    public StardewUiMenuBuilder View(string viewName)
    {
        if (string.IsNullOrWhiteSpace(viewName))
            throw new ArgumentException("View name cannot be null or empty.", nameof(viewName));
        
        _viewName = viewName;
        return this;
    }

    /// <summary>
    /// Sets the context provider for data binding.
    /// </summary>
    /// <param name="getContext">Function that returns the binding context.</param>
    /// <returns>This builder for chaining.</returns>
    public StardewUiMenuBuilder Context(Func<object?> getContext)
    {
        _contextProvider = getContext ?? throw new ArgumentNullException(nameof(getContext));
        return this;
    }

    /// <summary>
    /// Configures the menu controller with custom settings.
    /// </summary>
    /// <param name="configure">Action to configure the controller.</param>
    /// <returns>This builder for chaining.</returns>
    public StardewUiMenuBuilder ConfigureController(Action<IMenuController> configure)
    {
        _controllerConfig = configure ?? throw new ArgumentNullException(nameof(configure));
        return this;
    }

    /// <summary>
    /// Sets a callback to invoke when the menu is closing.
    /// </summary>
    /// <param name="callback">The callback action.</param>
    /// <returns>This builder for chaining.</returns>
    public StardewUiMenuBuilder OnClosing(Action callback)
    {
        _onClosing = callback ?? throw new ArgumentNullException(nameof(callback));
        return this;
    }

    /// <summary>
    /// Sets a callback to invoke when the menu has been closed.
    /// </summary>
    /// <param name="callback">The callback action.</param>
    /// <returns>This builder for chaining.</returns>
    public StardewUiMenuBuilder OnClosed(Action callback)
    {
        _onClosed = callback ?? throw new ArgumentNullException(nameof(callback));
        return this;
    }

    /// <summary>
    /// Gets the configured view name.
    /// </summary>
    internal string? ViewName => _viewName;

    /// <summary>
    /// Gets the context provider.
    /// </summary>
    internal Func<object?>? ContextProvider => _contextProvider;

    /// <summary>
    /// Gets the controller configuration action.
    /// </summary>
    internal Action<IMenuController>? ControllerConfig => _controllerConfig;

    /// <summary>
    /// Gets the closing callback.
    /// </summary>
    internal Action? OnClosingCallback => _onClosing;

    /// <summary>
    /// Gets the closed callback.
    /// </summary>
    internal Action? OnClosedCallback => _onClosed;
}

