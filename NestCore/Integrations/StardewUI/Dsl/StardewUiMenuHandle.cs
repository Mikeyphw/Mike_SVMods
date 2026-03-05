// FILE: NestCore/Integrations/StardewUI/Dsl/StardewUiMenuHandle.cs
// Handle for a managed StardewUI menu controller.

using System;
using StardewUI.Framework;
using StardewValley.Menus;

namespace Mikew.NestCore.Integrations.StardewUI.Dsl;

/// <summary>
/// Handle for a managed menu controller with deterministic lifecycle.
/// </summary>
public sealed class StardewUiMenuHandle : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Creates a new menu handle.
    /// </summary>
    /// <param name="controller">The underlying menu controller.</param>
    internal StardewUiMenuHandle(IMenuController controller)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    /// <summary>
    /// Gets the underlying menu controller.
    /// </summary>
    public IMenuController Controller { get; }

    /// <summary>
    /// Gets the underlying clickable menu.
    /// </summary>
    public IClickableMenu Menu => Controller.Menu;

    /// <summary>
    /// Closes the menu.
    /// </summary>
    public void Close()
    {
        if (_disposed)
            return;

        Controller.Close();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Controller.Dispose();
    }
}

