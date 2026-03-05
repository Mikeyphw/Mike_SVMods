// FILE: NestCore/Integrations/StardewUI/Dsl/StardewUiMenuHost.cs
// Host for managing StardewUI menu controllers with lifecycle management.

using System;
using System.Collections.Generic;
using System.Linq;
using StardewUI.Framework;
using StardewValley;
using StardewValley.Menus;

namespace Mikew.NestCore.Integrations.StardewUI.Dsl;

/// <summary>
/// Host for managing StardewUI menu controllers.
/// </summary>
public sealed class StardewUiMenuHost : IDisposable
{
    private readonly StardewUiHost _host;
    private readonly Dictionary<string, StardewUiMenuHandle> _handles = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new menu host.
    /// </summary>
    /// <param name="host">Parent StardewUI host.</param>
    /// <exception cref="ArgumentNullException">Thrown when host is null.</exception>
    public StardewUiMenuHost(StardewUiHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    /// <summary>
    /// Opens a menu as the active menu.
    /// </summary>
    /// <param name="menuId">The menu ID (must be defined in the layout).</param>
    /// <returns>The menu handle, or null if the menu ID is not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when menuId is null.</exception>
    public StardewUiMenuHandle? OpenAsActiveMenu(string menuId)
    {
        ArgumentNullException.ThrowIfNull(menuId);

        if (_disposed)
            throw new ObjectDisposedException(nameof(StardewUiMenuHost));

        CloseActiveIfOwned();

        if (_handles.TryGetValue(menuId, out var existing))
            existing.Dispose();

        // Create the controller from the host
        var controller = _host.CreateMenuController(menuId);
        if (controller is null)
            return null;

        var handle = new StardewUiMenuHandle(controller, disposedHandle => RemoveHandle(menuId, disposedHandle));
        _handles[menuId] = handle;

        // Set as active menu
        Game1.activeClickableMenu = controller.Menu;

        return handle;
    }

    /// <summary>
    /// Closes the active menu if it was opened by this host.
    /// </summary>
    public void CloseActiveIfOwned()
    {
        if (_disposed)
            return;

        if (Game1.activeClickableMenu is null)
            return;

        // Check if the active menu is owned by any of our handles
        foreach (var handle in _handles.Values)
        {
            if (handle.Menu == Game1.activeClickableMenu)
            {
                handle.Close();
                Game1.activeClickableMenu = null;
                handle.Dispose();
                return;
            }
        }
    }

    /// <summary>
    /// Gets a handle by ID, or null if not found.
    /// </summary>
    /// <param name="menuId">The menu ID.</param>
    /// <returns>The handle, or null if not open.</returns>
    public StardewUiMenuHandle? Get(string menuId)
    {
        _handles.TryGetValue(menuId, out var handle);
        return handle;
    }

    /// <summary>
    /// Checks if a menu is currently open.
    /// </summary>
    /// <param name="menuId">The menu ID.</param>
    /// <returns>True if open.</returns>
    public bool IsOpen(string menuId) => _handles.ContainsKey(menuId);

    /// <summary>
    /// Disposes all menu handles.
    /// </summary>
    public void DisposeAll()
    {
        if (_disposed)
            return;

        foreach (var handle in _handles.Values.ToArray())
        {
            handle.Dispose();
        }
    }

    private void RemoveHandle(string menuId, StardewUiMenuHandle handle)
    {
        if (_handles.TryGetValue(menuId, out var current) && ReferenceEquals(current, handle))
            _handles.Remove(menuId);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        DisposeAll();
    }
}
