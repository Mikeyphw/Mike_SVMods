using StardewModdingAPI;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using Mikew.NestCore.Helpers.Content;
using Mikew.NestCore;

namespace EggsInWild.Infrastructure.Content.Editors;

/// <summary>
/// Patches the incubator sprite when fern is not required.
/// The incubator texture contains a "no-fern" variant at y=32 (16x32), copied onto the primary sprite at y=0.
/// </summary>
public sealed class WildEggIncubatorTextureEditor : AssetEditorBase<ContentContext>
{
    public override string AssetName => $"Mods/{Constants.ModId}/bigcraftables/wildeggincubator";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        if (context.Config.NeedsFern)
            return;

        var editor = asset.AsImage();

        // Load the raw incubator texture from disk and patch the no-fern sprite over the default.
        IRawTextureData sourceImage = context.Helper.ModContent.Load<IRawTextureData>(
            "assets/bigcraftables/wildeggincubator.png"
        );

        editor.PatchImage(
            sourceImage,
            sourceArea: new Rectangle(0, 32, 16, 32),
            targetArea: new Rectangle(0, 0, 16, 32)
        );
    }
}
