#!/usr/bin/env bash
set -euo pipefail

fail() { echo "ERROR: $1" >&2; exit 1; }

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
core="$root/Core"

[[ -d "$core" ]] || fail "Core folder missing: $core"

# Core is a domain layer. Detect source-level references to runtime namespaces,
# rather than arbitrary words in comments/documentation (for example "no SMAPI").
# Report every violating file in one pass so remediation is deterministic.
violations=()
while IFS= read -r -d '' f; do
  matches="$({
    grep -n -E '^[[:space:]]*(global[[:space:]]+)?using[[:space:]]+((static[[:space:]]+)?|[[:alnum:]_]+[[:space:]]*=[[:space:]]*)(global::)?(StardewValley|StardewModdingAPI|Microsoft\.Xna\.Framework)([.;[:space:]]|$)' "$f" || true
    grep -n -E '(^|[^[:alnum:]_])((global::)?(StardewValley|StardewModdingAPI|Microsoft\.Xna\.Framework))\.' "$f" \
      | grep -v -E '^[0-9]+:[[:space:]]*(//|///|\*)' || true
  } | sort -t: -k1,1n -u)"

  if [[ -n "$matches" ]]; then
    while IFS= read -r match; do
      [[ -n "$match" ]] || continue
      violations+=("$f:$match")
    done <<< "$matches"
  fi
done < <(find "$core" -type f -name '*.cs' -print0 | sort -z)

if (( ${#violations[@]} > 0 )); then
  echo "ERROR: Core purity violations (${#violations[@]} reference(s)):" >&2
  printf '  - %s\n' "${violations[@]}" >&2
  exit 1
fi

duplicates=(
  "SmapiConfigProvider.cs"
  "ILog.cs"
  "IAssetEditor.cs"
  "AssetEditorBase.cs"
  "SmapiContentCoordinator.cs"
  "GameAssets.cs"
)

duplicate_violations=()
for name in "${duplicates[@]}"; do
  while IFS= read -r -d '' hit; do
    duplicate_violations+=("$name: $hit")
  done < <(find "$root" -type f -name "$name" -print0 2>/dev/null)
done

if (( ${#duplicate_violations[@]} > 0 )); then
  echo "ERROR: NestCore duplicate regression (${#duplicate_violations[@]} file(s)):" >&2
  printf '  - %s\n' "${duplicate_violations[@]}" >&2
  exit 1
fi

echo "OK: Core purity + NestCore duplicate checks passed."
