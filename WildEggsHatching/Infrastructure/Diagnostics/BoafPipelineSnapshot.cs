using System;
using System.Collections.Generic;

namespace WildEggsHatching.Infrastructure.Diagnostics;

/// <summary>Runtime BOAF wiring snapshot used by wegg verify.</summary>
public static class BoafPipelineSnapshot
{
    private static readonly object Sync = new();

    public static bool IsAttached { get; private set; }

    public static IReadOnlyList<string> EditorAssets { get; private set; } = Array.Empty<string>();

    public static IReadOnlyList<string> ModAssetPaths { get; private set; } = Array.Empty<string>();

    public static void Set(bool attached, IReadOnlyList<string> editorAssets, IReadOnlyList<string> modAssetPaths)
    {
        lock (Sync)
        {
            IsAttached = attached;
            EditorAssets = editorAssets;
            ModAssetPaths = modAssetPaths;
        }
    }
}
