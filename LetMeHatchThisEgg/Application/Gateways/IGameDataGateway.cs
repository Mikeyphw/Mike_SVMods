namespace LetMeHatchThisEgg.Application.Gateways;

/// <summary>Abstracts loading game data assets by name.</summary>
public interface IGameDataGateway
{
    T Load<T>(string assetName) where T : notnull;
}
