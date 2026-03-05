// FILE: NestCore/Integrations/StardewUI/Dsl/StardewUiHudHost.cs
// Host for managing StardewUI HUD drawables with event wiring.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewUI.Framework;

namespace Mikew.NestCore.Integrations.StardewUI.Dsl;

/// <summary>
/// Host for managing StardewUI HUD drawables.
/// </summary>
public sealed class StardewUiHudHost : IDisposable
{
    private readonly IModHelper _helper;
    private readonly StardewUiHost _host;
    private readonly Dictionary<string, StardewUiHudHandle> _handles = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new HUD host.
    /// </summary>
    /// <param name="helper">SMAPI helper.</param>
    /// <param name="host">Parent StardewUI host.</param>
    /// <exception cref="ArgumentNullException">Thrown when helper or host is null.</exception>
    public StardewUiHudHost(IModHelper helper, StardewUiHost host)
    {
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        _host = host ?? throw new ArgumentNullException(nameof(host));

        // Wire to RenderedHud event
        _helper.Events.Display.RenderedHud += OnRenderedHud;
    }

    /// <summary>
    /// Shows a HUD by ID.
    /// </summary>
    /// <param name="hudId">The HUD ID (must be defined in the layout).</param>
    /// <param name="getPosition">Function to get the draw position.</param>
    /// <returns>The HUD handle, or null if the HUD ID is not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when hudId or getPosition is null.</exception>
    public StardewUiHudHandle? Show(string hudId, Func<Vector2> getPosition)
    {
        ArgumentNullException.ThrowIfNull(hudId);
        ArgumentNullException.ThrowIfNull(getPosition);

        if (_disposed)
            throw new ObjectDisposedException(nameof(StardewUiHudHost));

        // Check if already shown
        if (_handles.TryGetValue(hudId, out var existing))
            return existing;

        // Create the drawable from the host
        var drawable = _host.CreateHud(hudId);
        if (drawable is null)
            return null;

        var handle = new StardewUiHudHandle(drawable, getPosition);
        _handles[hudId] = handle;
        return handle;
    }

    /// <summary>
    /// Hides a HUD by ID.
    /// </summary>
    /// <param name="hudId">The HUD ID.</param>
    public void Hide(string hudId)
    {
        if (_disposed)
            return;

        if (_handles.TryGetValue(hudId, out var handle))
        {
            handle.Dispose();
            _handles.Remove(hudId);
        }
    }

    /// <summary>
    /// Hides all HUDs.
    /// </summary>
    public void HideAll()
    {
        if (_disposed)
            return;

        foreach (var handle in _handles.Values)
        {
            handle.Dispose();
        }
        _handles.Clear();
    }

    /// <summary>
    /// Gets a handle by ID, or null if not found.
    /// </summary>
    /// <param name="hudId">The HUD ID.</param>
    /// <returns>The handle, or null if not shown.</returns>
    public StardewUiHudHandle? Get(string hudId)
    {
        _handles.TryGetValue(hudId, out var handle);
        return handle;
    }

    /// <summary>
    /// Checks if a HUD is currently shown.
    /// </summary>
    /// <param name="hudId">The HUD ID.</param>
    /// <returns>True if shown.</returns>
    public bool IsShown(string hudId) => _handles.ContainsKey(hudId);

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (_disposed)
            return;

        foreach (var handle in _handles.Values)
        {
            handle.Draw(e.SpriteBatch);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Unsubscribe from events
        _helper.Events.Display.RenderedHud -= OnRenderedHud;

        // Dispose all handles
        HideAll();
    }
}

