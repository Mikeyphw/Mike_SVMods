using EggsInWild.Core.Enums;

namespace EggsInWild.Infrastructure.Diagnostics;

public record WeggVerifyOptions(
    bool AsJson,
    bool Full,
    bool IsWorldReady,
    bool IsMainPlayer,
    ProgressionStep ProgressionStep
);

