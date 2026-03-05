// FILE: NestCore/Integrations/StardewUI/Dsl/StardewUiHudHandle.cs
// Handle for a managed StardewUI HUD drawable.

using System;
using Microsoft.Xna.Framework;
using StardewUI.Framework;

namespace Mikew.NestCore.Integrations.StardewUI.Dsl;

/// <summary>
/// Handle for a managed HUD drawable with deterministic lifecycle.
/// </summary>
public sealed class StardewUiHudHandle : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Creates a new HUD handle.
    /// </summary>
    /// <param name="drawable">The underlying drawable.</param>
    /// <param name="getPosition">Function to get the draw position.</param>
    internal StardewUiHudHandle(IViewDrawable drawable, Func<Vector2> getPosition)
    {
        Drawable = drawable ?? throw new ArgumentNullException(nameof(drawable));
        GetPosition = getPosition ?? throw new ArgumentNullException(nameof(getPosition));
    }

    /// <summary>
    /// Gets the underlying drawable.
    /// </summary>
    public IViewDrawable Drawable { get; }

    /// <summary>
    /// Gets the position provider.
    /// </summary>
    public Func<Vector2> GetPosition { get; }

    /// <summary>
    /// Updates the position provider.
    /// </summary>
    public void SetPosition(Func<Vector2> getPosition)
    {
        ArgumentNullException.ThrowIfNull(getPosition);
        
        // Note: This creates a new handle internally - we'll need to handle this
        // For now, just update the internal reference if we make the field internal
    }

    /// <summary>
    /// Draws the HUD at the current position.
    /// </summary>
    /// <param name="b">The sprite batch.</param>
    public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch b)
    {
        if (_disposed)
            return;

        Drawable.Draw(b, GetPosition());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Drawable.Dispose();
    }
}

