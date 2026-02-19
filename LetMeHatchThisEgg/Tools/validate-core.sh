#!/usr/bin/env bash
set -euo pipefail

fail() { echo "ERROR: $1" >&2; exit 1; }

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
core="$root/Core"

[[ -d "$core" ]] || fail "Core folder missing: $core"

forbidden=(
  "StardewValley"
  "StardewModdingAPI"
  "SMAPI"
  "Microsoft.Xna.Framework"
)

while IFS= read -r -d '' f; do
  for token in "${forbidden[@]}"; do
    if grep -n -F "$token" "$f" >/dev/null 2>&1; then
      fail "Core purity violation in $f: contains '$token'"
    fi
  done
done < <(find "$core" -type f -name '*.cs' -print0)

duplicates=(
  "SmapiConfigProvider.cs"
  "ILog.cs"
  "IAssetEditor.cs"
  "AssetEditorBase.cs"
  "SmapiContentCoordinator.cs"
  "GameAssets.cs"
)

for name in "${duplicates[@]}"; do
  hits="$(find "$root" -type f -name "$name" 2>/dev/null || true)"
  if [[ -n "$hits" ]]; then
    fail "BOAF duplicate regression: found $name:\n$hits"
  fi
done

echo "OK: Core purity + BOAF duplicate checks passed."
