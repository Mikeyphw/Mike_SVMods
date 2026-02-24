using System.Collections.Generic;
using RoosterControl.Application.Catalogs;

namespace RoosterControl.Application.Catalogs.Console;

/// <summary>Represents the per-file status of a catalog operation.</summary>
internal enum CatalogFileStatus
{
    Ok,
    Migrated,
    Error
}

/// <summary>Represents the validation outcome for a single catalog file.</summary>
internal sealed record CatalogFileReport(
    string Name,
    string RelativePath,
    CatalogFileStatus Status,
    bool WouldChange,
    IReadOnlyList<string> Issues
);

/// <summary>Represents the outcome of running catalog validation (and optional fix).</summary>
internal sealed record CatalogValidateResult(
    IReadOnlyList<CatalogFileReport> Reports,
    string? BackupRelativeDir,
    string? InvalidRelativeDir,
    IReadOnlyList<CatalogIssue> Issues,
    IReadOnlyList<CatalogIssue> PostFixIssues
);
