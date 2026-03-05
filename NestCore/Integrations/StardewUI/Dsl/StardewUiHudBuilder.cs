// FILE: NestCore/Integrations/StardewUI/Dsl/StardewUiHudBuilder.cs
// DSL builder for StardewUI HUD drawables.

using System;
using Microsoft.Xna.Framework;

namespace Mikew.NestCore.Integrations.StardewUI.Dsl;

/// <summary>
/// Builder for configuring a StardewUI HUD drawable.
/// </summary>
public sealed class StardewUiHudBuilder
{
    private string? _viewName;
    private Func<object?>? _contextProvider;
    private Func<Vector2?>? _maxSizeProvider;

    /// <summary>
    /// Sets the view name for this HUD. Required - throws if not set.
    /// </summary>
    /// <param name="viewName">The view name (short name or qualified asset name).</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if view name is not set.</exception>
    public StardewUiHudBuilder View(string viewName)
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
    public StardewUiHudBuilder Context(Func<object?> getContext)
    {
        _contextProvider = getContext ?? throw new ArgumentNullException(nameof(getContext));
        return this;
    }

    /// <summary>
    /// Sets the maximum size for the HUD drawable.
    /// </summary>
    /// <param name="getMaxSize">Function that returns the maximum size, or null for no constraint.</param>
    /// <returns>This builder for chaining.</returns>
    public StardewUiHudBuilder MaxSize(Func<Vector2?> getMaxSize)
    {
        _maxSizeProvider = getMaxSize ?? throw new ArgumentNullException(nameof(getMaxSize));
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
    /// Gets the max size provider.
    /// </summary>
    internal Func<Vector2?>? MaxSizeProvider => _maxSizeProvider;
}

