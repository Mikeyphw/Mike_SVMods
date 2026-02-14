using StardewModdingAPI.Events;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class MailEditor : AssetEditorBase
{
    public override string AssetName => GameAssets.Mail;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        _ = asset.AsDictionary<string, string>().Data;
        context.Log("MailEditor applied (Phase 0 wiring).");
    }
}
