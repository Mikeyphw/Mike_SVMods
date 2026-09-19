param()

$ErrorActionPreference = "Stop"

function Fail([string]$msg) {
  Write-Host "ERROR: $msg"
  exit 1
}

$root = Split-Path -Parent $PSScriptRoot
$core = Join-Path $root "Core"

if (-not (Test-Path $core)) { Fail "Core folder missing: $core" }

# Core is a domain layer. Detect source-level references to runtime namespaces,
# rather than arbitrary words in comments/documentation (for example "no SMAPI").
# Report every violating file in one pass so remediation is deterministic.
$usingPattern = '^\s*(?:global\s+)?using\s+(?:(?:static\s+)?|[A-Za-z_][A-Za-z0-9_]*\s*=\s*)(?:global::)?(?:StardewValley|StardewModdingAPI|Microsoft\.Xna\.Framework)(?:[.;\s]|$)'
$qualifiedPattern = '(?:^|[^A-Za-z0-9_])(?:(?:global::)?(?:StardewValley|StardewModdingAPI|Microsoft\.Xna\.Framework))\.'
$commentOnlyPattern = '^\s*(?://|///|\*)'
$violations = [System.Collections.Generic.List[string]]::new()

$csFiles = Get-ChildItem -Path $core -Recurse -Filter *.cs | Sort-Object FullName
foreach ($f in $csFiles) {
  $lineNumber = 0
  foreach ($line in Get-Content -Path $f.FullName) {
    $lineNumber++
    if (($line -match $usingPattern) -or (($line -match $qualifiedPattern) -and ($line -notmatch $commentOnlyPattern))) {
      $violations.Add("$($f.FullName):$lineNumber:$($line.Trim())")
    }
  }
}

if ($violations.Count -gt 0) {
  Write-Host "ERROR: Core purity violations ($($violations.Count) reference(s)):"
  foreach ($violation in $violations) { Write-Host "  - $violation" }
  exit 1
}

$duplicateFiles = @(
  "SmapiConfigProvider.cs",
  "ILog.cs",
  "IAssetEditor.cs",
  "AssetEditorBase.cs",
  "SmapiContentCoordinator.cs",
  "GameAssets.cs"
)

$duplicateViolations = [System.Collections.Generic.List[string]]::new()
foreach ($name in $duplicateFiles) {
  $hits = Get-ChildItem -Path $root -Recurse -Filter $name -ErrorAction SilentlyContinue
  foreach ($hit in $hits) {
    $duplicateViolations.Add("$name: $($hit.FullName)")
  }
}

if ($duplicateViolations.Count -gt 0) {
  Write-Host "ERROR: NestCore duplicate regression ($($duplicateViolations.Count) file(s)):"
  foreach ($violation in $duplicateViolations) { Write-Host "  - $violation" }
  exit 1
}

Write-Host "OK: Core purity + NestCore duplicate checks passed."
