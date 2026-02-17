using System;
using Microsoft.Xna.Framework;

namespace WildEggsHatching.Infrastructure.DesertTomb.Diagnostics;

/// <summary>
/// Pure math helpers for rendering the Pharaoh HUD tracker arrow.
/// </summary>
public static class PharaohTrackerMath
{
    private const float EdgePaddingPx = 8f;

    /// <summary>
    /// Computes the on-screen position and rotation for an off-screen indicator arrow.
    /// </summary>
    /// <param name="worldCenterPx">Target world position in pixels (usually tile*64 + (32,32)).</param>
    /// <param name="viewportWorld">Viewport rectangle in world coordinates (pixels).</param>
    /// <param name="screenBounds">Screen bounds in screen coordinates.</param>
    /// <param name="marginPx">How far outside the viewport is still considered "on-screen".</param>
    /// <param name="screenPosition">Result position in screen coordinates.</param>
    /// <param name="rotation">Result rotation in radians.</param>
    /// <returns><c>true</c> if the arrow should be drawn (target is off-screen).</returns>
    public static bool TryComputeArrow(
        Vector2 worldCenterPx,
        Rectangle viewportWorld,
        Rectangle screenBounds,
        int marginPx,
        out Vector2 screenPosition,
        out float rotation
    )
    {
        var rel = worldCenterPx - new Vector2(viewportWorld.X, viewportWorld.Y);

        if (rel.X >= -marginPx
            && rel.X <= screenBounds.Width + marginPx
            && rel.Y >= -marginPx
            && rel.Y <= screenBounds.Height + marginPx)
        {
            screenPosition = default;
            rotation = 0f;
            return false;
        }

        screenPosition = default;
        rotation = 0f;

        if (worldCenterPx.X > viewportWorld.Right - marginPx)
        {
            screenPosition.X = screenBounds.Right - EdgePaddingPx;
            rotation = MathF.PI / 2f;
        }
        else if (worldCenterPx.X < viewportWorld.X)
        {
            screenPosition.X = EdgePaddingPx;
            rotation = -(MathF.PI / 2f);
        }
        else
        {
            screenPosition.X = rel.X;
        }

        if (worldCenterPx.Y > viewportWorld.Bottom - marginPx)
        {
            screenPosition.Y = screenBounds.Bottom - EdgePaddingPx;
            rotation = MathF.PI;
        }
        else if (worldCenterPx.Y < viewportWorld.Y)
        {
            screenPosition.Y = EdgePaddingPx;
        }
        else
        {
            screenPosition.Y = rel.Y;
        }

        // Corner tweaks to avoid pointing "too" cardinal when the target is diagonally off-screen.
        if (screenPosition.X == EdgePaddingPx && screenPosition.Y == EdgePaddingPx)
            rotation += MathF.PI / 4f;

        if (screenPosition.X == EdgePaddingPx && screenPosition.Y == screenBounds.Bottom - EdgePaddingPx)
            rotation += MathF.PI / 4f;

        if (screenPosition.X == screenBounds.Right - EdgePaddingPx && screenPosition.Y == EdgePaddingPx)
            rotation -= MathF.PI / 4f;

        if (screenPosition.X == screenBounds.Right - EdgePaddingPx && screenPosition.Y == screenBounds.Bottom - EdgePaddingPx)
            rotation -= MathF.PI / 4f;

        return true;
    }

    public static Vector2 ClampToScreen(Vector2 pos, Rectangle screenBounds, Vector2 renderSize)
    {
        float maxX = Math.Max(0f, screenBounds.Width - renderSize.X);
        float maxY = Math.Max(0f, screenBounds.Height - renderSize.Y);

        return new Vector2(
            Math.Clamp(pos.X, 0f, maxX),
            Math.Clamp(pos.Y, 0f, maxY)
        );
    }
}
