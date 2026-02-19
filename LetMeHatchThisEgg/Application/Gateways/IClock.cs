using System;

namespace LetMeHatchThisEgg.Application.Gateways;

/// <summary>Abstracts access to wall-clock time for application logic.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
