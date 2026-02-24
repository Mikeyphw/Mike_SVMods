using System;

namespace LetMeHatch.Application.Gateways;

/// <summary>Abstracts access to wall-clock time for application logic.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
