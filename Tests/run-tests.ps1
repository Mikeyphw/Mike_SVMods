$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$tests = Join-Path $root "Tests"

Write-Host "== Validate Core (LMH) =="
$validator = Join-Path $root "LetMeHatch\Tools\Validate-Core.ps1"
if (Test-Path $validator) {
  & $validator
} else {
  Write-Warning "Validate-Core.ps1 not found; skipping."
}

Write-Host "== dotnet test =="
dotnet test (Join-Path $tests "Tests.sln") -c Release
