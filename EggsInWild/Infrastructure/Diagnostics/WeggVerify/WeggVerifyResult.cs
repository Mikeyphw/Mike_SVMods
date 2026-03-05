namespace EggsInWild.Infrastructure.Diagnostics;

public record WeggVerifyResult(bool Success, string Rendered, WeggVerifyReport Report);

