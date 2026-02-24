namespace LetMeHatch.Application.Gateways;

/// <summary>Abstracts asset cache invalidation.</summary>
public interface IAssetGateway
{
    void Invalidate(string assetName);
}
