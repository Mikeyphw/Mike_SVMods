using System;
using LetMeHatch.Application.Gateways;

namespace LetMeHatch.Infrastructure.Gateways;

/// <summary>SMAPI-backed clock.</summary>
public sealed class SmapiClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
