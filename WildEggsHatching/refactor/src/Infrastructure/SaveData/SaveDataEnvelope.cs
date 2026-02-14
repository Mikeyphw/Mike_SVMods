namespace WildEggsHatching.Infrastructure.SaveData;

/// <summary>Versioned save-data envelope to support future migrations.</summary>
public sealed record SaveDataEnvelope<T>(int Version, T Data);
