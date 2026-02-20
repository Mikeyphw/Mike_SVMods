using System;
using System.Collections.Generic;
using System.Linq;

namespace WildEggsHatching.Core.ShopProgression;

public static class ConditionJoiner
{
    public static string? And(params string?[] parts)
    {
        var tokens = new List<string>();
        foreach (var p in parts ?? Array.Empty<string?>())
        {
            var t = (p ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(t))
                continue;

            if (!tokens.Contains(t, StringComparer.OrdinalIgnoreCase))
                tokens.Add(t);
        }

        return tokens.Count == 0 ? null : string.Join(", ", tokens);
    }

    public static string MailReceivedByHost(string mailId)
        => $"PLAYER_HAS_MAIL Host {mailId} Received";
}
