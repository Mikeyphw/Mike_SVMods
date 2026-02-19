using System;
using LetMeHatchThisEgg.Application.Gateways;

namespace LetMeHatchThisEgg.Infrastructure.Gateways;

/// <summary>SMAPI-backed clock.</summary>
public sealed class SmapiClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
