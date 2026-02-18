using StardewModdingAPI.Events;

namespace LetMeHatchThisEgg.Infrastructure.Content;

public interface IAssetEditor
{
    string AssetName { get; }
    AssetEditPriority Priority { get; }

    void Edit(IAssetData asset, ContentContext context);
}
