$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$tests = Join-Path $root "Tests"

Write-Host "== Validate Core (LMH) =="
$validator = Join-Path $root "LetMeHatch\Tools\Validate-Core.ps1"
if (-not (Test-Path -LiteralPath $validator -PathType Leaf)) {
  throw "Required Core validator is missing: $validator"
}
# Native command exit codes are not consistently converted into terminating
# exceptions across PowerShell versions. Fail closed on either step.
$global:LASTEXITCODE = 0
& $validator
if ($LASTEXITCODE -ne 0) {
  throw "Core validation failed with exit code $LASTEXITCODE"
}

Write-Host "== dotnet test =="
& dotnet test (Join-Path $tests "Tests.sln") -c Release
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}
