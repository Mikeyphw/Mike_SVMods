using System.Collections.Generic;

namespace Mikew.NestCore.Diagnostics;

public sealed record DiagnosticIssue(
    string Code,
    DiagnosticSeverity Severity,
    string Area,
    string Message,
    string? Fix = null,
    IReadOnlyDictionary<string, string>? Data = null);
