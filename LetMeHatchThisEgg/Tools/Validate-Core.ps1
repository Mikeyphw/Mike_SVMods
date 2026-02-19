param()

$ErrorActionPreference = "Stop"

function Fail([string]$msg) {
  Write-Host "ERROR: $msg"
  exit 1
}

$root = Split-Path -Parent $PSScriptRoot
$core = Join-Path $root "Core"

if (-not (Test-Path $core)) { Fail "Core folder missing: $core" }

$forbidden = @(
  "StardewValley",
  "StardewModdingAPI",
  "SMAPI",
  "Microsoft.Xna.Framework"
)

$csFiles = Get-ChildItem -Path $core -Recurse -Filter *.cs
foreach ($f in $csFiles) {
  $text = Get-Content -Path $f.FullName -Raw
  foreach ($token in $forbidden) {
    if ($text -match [regex]::Escape($token)) {
      Fail "Core purity violation in $($f.FullName): contains '$token'"
    }
  }
}

$duplicateFiles = @(
  "SmapiConfigProvider.cs",
  "ILog.cs",
  "IAssetEditor.cs",
  "AssetEditorBase.cs",
  "SmapiContentCoordinator.cs",
  "GameAssets.cs"
)

foreach ($name in $duplicateFiles) {
  $hits = Get-ChildItem -Path $root -Recurse -Filter $name -ErrorAction SilentlyContinue
  if ($hits) {
    $paths = ($hits | ForEach-Object { $_.FullName }) -join "`n  - "
    Fail "BOAF duplicate regression: found $name:`n  - $paths"
  }
}

Write-Host "OK: Core purity + BOAF duplicate checks passed."
