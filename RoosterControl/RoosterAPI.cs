using System;
using System.Collections.Generic;
using Mikew.NestCore.Framework;

namespace RoosterControl;

public sealed class RoosterHatchingApi : IRoosterControlApi
{
    private static RoosterHatchingApi? _instance;

    public static RoosterHatchingApi Instance => _instance ??= new RoosterHatchingApi();

    private readonly Dictionary<string, string> _resolvedEggItemIds = new(StringComparer.OrdinalIgnoreCase);

    public int ApiVersion => 3;

    public IReadOnlyDictionary<string, string> GetResolvedEggItemIds()
        => new Dictionary<string, string>(_resolvedEggItemIds, StringComparer.OrdinalIgnoreCase);

    public void UpdateResolvedEggItemIds(IReadOnlyDictionary<string, string> itemIds)
    {
        _resolvedEggItemIds.Clear();
        foreach (var (eggKind, itemId) in itemIds)
        {
            if (string.IsNullOrWhiteSpace(eggKind) || string.IsNullOrWhiteSpace(itemId))
                continue;

            _resolvedEggItemIds[eggKind] = itemId;
        }
    }

    private RoosterHatchingApi() { }
}
