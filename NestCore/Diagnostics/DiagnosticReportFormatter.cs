using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Mikew.NestCore.Diagnostics;

public static class DiagnosticReportFormatter
{
    public static string FormatText(DiagnosticReport report, string? title = null)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(title))
            lines.Add(title);

        lines.Add($"PASS={report.PassCount} WARN={report.WarnCount} FAIL={report.FailCount}");

        foreach (var issue in report.Issues)
        {
            lines.Add($"[{issue.Severity.ToString().ToUpperInvariant()}][{issue.Area}][{issue.Code}] {issue.Message}");

            if (!string.IsNullOrWhiteSpace(issue.Fix))
                lines.Add($"  Fix: {issue.Fix}");

            if (issue.Data is not null && issue.Data.Count > 0)
            {
                var payload = string.Join(", ", issue.Data.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"));
                lines.Add($"  Data: {payload}");
            }
        }

        return string.Join("\n", lines);
    }

    public static string FormatJson(DiagnosticReport report, string? title = null)
    {
        var payload = new
        {
            Title = title,
            Summary = new
            {
                PASS = report.PassCount,
                WARN = report.WarnCount,
                FAIL = report.FailCount,
            },
            Issues = report.Issues.Select(issue => new
            {
                issue.Code,
                Severity = issue.Severity.ToString().ToUpperInvariant(),
                issue.Area,
                issue.Message,
                issue.Fix,
                issue.Data,
            }),
        };

        return JsonConvert.SerializeObject(payload, Formatting.Indented);
    }
}
