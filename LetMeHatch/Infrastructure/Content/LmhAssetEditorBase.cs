using Mikew.NestCore.Helpers.Content;
using StardewModdingAPI.Events;

namespace LetMeHatch.Infrastructure.Content;

/// <summary>LMH asset editor base: NestCore editor abstraction + LMH debug metadata.</summary>
public abstract class LmhAssetEditorBase : AssetEditorBase<ContentContext>, IContentHandlerDebugInfo
{
    public virtual string DebugHandles => AssetName;
    public virtual string DebugGates => "Always; no explicit gate.";
    public virtual string DebugName => GetType().Name;
    public virtual string[]? DebugTags => null;
    public virtual string? DebugSummary => null;
}
