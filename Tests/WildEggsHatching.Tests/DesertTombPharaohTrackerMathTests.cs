using System;
using Microsoft.Xna.Framework;
using WildEggsHatching.Infrastructure.DesertTomb.Diagnostics;
using Xunit;

namespace WildEggsHatching.Tests;

public sealed class DesertTombPharaohTrackerMathTests
{
    [Fact]
    public void TryComputeArrow_ReturnsFalse_WhenTargetIsOnScreen()
    {
        var viewportWorld = new Rectangle(100, 200, 800, 600);
        var screenBounds = new Rectangle(0, 0, 800, 600);

        var worldCenterPx = new Vector2(100 + 400, 200 + 300);

        var draw = PharaohTrackerMath.TryComputeArrow(
            worldCenterPx,
            viewportWorld,
            screenBounds,
            marginPx: 64,
            out _,
            out _);

        Assert.False(draw);
    }

    [Fact]
    public void TryComputeArrow_RightOffscreen_PlacesArrowOnRightEdge()
    {
        var viewportWorld = new Rectangle(0, 0, 800, 600);
        var screenBounds = new Rectangle(0, 0, 800, 600);

        var worldCenterPx = new Vector2(viewportWorld.Right + 200, 300);

        var draw = PharaohTrackerMath.TryComputeArrow(
            worldCenterPx,
            viewportWorld,
            screenBounds,
            marginPx: 64,
            out var pos,
            out var rot);

        Assert.True(draw);
        Assert.Equal(screenBounds.Right - 8f, pos.X);
        Assert.InRange(rot, (float)(Math.PI / 2f) - 0.0001f, (float)(Math.PI / 2f) + 0.0001f);
    }

    [Fact]
    public void TryComputeArrow_TopLeftOffscreen_AppliesCornerRotationTweak()
    {
        var viewportWorld = new Rectangle(500, 500, 800, 600);
        var screenBounds = new Rectangle(0, 0, 800, 600);

        var worldCenterPx = new Vector2(viewportWorld.X - 200, viewportWorld.Y - 200);

        var draw = PharaohTrackerMath.TryComputeArrow(
            worldCenterPx,
            viewportWorld,
            screenBounds,
            marginPx: 64,
            out var pos,
            out var rot);

        Assert.True(draw);
        Assert.Equal(8f, pos.X);
        Assert.Equal(8f, pos.Y);

        // rotation starts at -PI/2 (left) then + PI/4 corner tweak => -PI/4
        Assert.InRange(rot, (float)(-Math.PI / 4f) - 0.0001f, (float)(-Math.PI / 4f) + 0.0001f);
    }
}
