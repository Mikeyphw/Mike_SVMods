using System.Collections.Generic;
using System.Linq;

namespace Mikew.NestCore.Diagnostics;

public sealed class DiagnosticReport
{
    private readonly List<DiagnosticIssue> _issues = new();

    public DiagnosticReport()
    {
    }

    public DiagnosticReport(IEnumerable<DiagnosticIssue> issues)
    {
        _issues.AddRange(issues);
    }

    public DiagnosticReport(string _title, IEnumerable<DiagnosticIssue> issues)
    {
        _issues.AddRange(issues);
    }

    public IReadOnlyList<DiagnosticIssue> Issues => _issues;

    public int PassCount => _issues.Count(x => x.Severity == DiagnosticSeverity.Pass);
    public int WarnCount => _issues.Count(x => x.Severity == DiagnosticSeverity.Warn);
    public int FailCount => _issues.Count(x => x.Severity == DiagnosticSeverity.Fail);

    public void Add(DiagnosticIssue issue)
    {
        _issues.Add(issue);
    }

    public void Add(
        string code,
        DiagnosticSeverity severity,
        string area,
        string message,
        string? fix = null,
        IReadOnlyDictionary<string, string>? data = null)
    {
        _issues.Add(new DiagnosticIssue(code, severity, area, message, fix, data));
    }
}
