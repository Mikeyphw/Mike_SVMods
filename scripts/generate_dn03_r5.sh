#!/usr/bin/env bash
set -euo pipefail

# Run from the existing Mike_SVMods checkout, after both overlays committed.
ROOT="$(git rev-parse --show-toplevel)"
DEVTOOL_SOURCE="${1:-$HOME/Code/devtool}"
PIN="$(git -C "$DEVTOOL_SOURCE" rev-parse HEAD)"
EXPECTED_BASE=df2d306045619bc096c3bd18a68c3fb85023e4dd
if test "$PIN" = "$EXPECTED_BASE"; then
  echo 'DN03_R5_REFUSED: Devtool R5 overlay has not been committed' >&2
  exit 2
fi
if test -n "$(git -C "$ROOT" status --porcelain)"; then
  echo 'DN03_R5_REFUSED: consumer must be clean before generating' >&2
  exit 2
fi
python3 "$ROOT/scripts/dn03_r5_source_contract.py" "$ROOT"
export PYTHONPATH="$DEVTOOL_SOURCE${PYTHONPATH:+:$PYTHONPATH}"
python3 -m devtool.actions_dn03_r4 plan --repo "$ROOT" --target dn03_r5 --workflow native --devtool-sha "$PIN"
python3 -m devtool.actions_dn03_r4 write --repo "$ROOT" --target dn03_r5 --workflow native --devtool-sha "$PIN" --yes
python3 -m devtool.actions_dn03_r4 verify --repo "$ROOT" --target dn03_r5 --workflow native --devtool-sha "$PIN"
YML=.github/workflows/devtool-dn03r4-dn03_r5-native.yml
RECEIPT=docs/ci/devtool-dn03r4-dn03_r5-native.json
cd "$ROOT"
git add -- "$YML" "$RECEIPT"
git diff --cached --check
git commit -m 'ci(rm09): commit generated DN03-R5 hosted native .NET jobs'
python3 -m devtool.actions_dn03_r4 verify --repo "$ROOT" --target dn03_r5 --workflow native --devtool-sha "$PIN"
printf '\nDN03_R5_GENERATED_COMMITTED=PASS\nDEVTOOL_SOURCE_SHA=%s\n' "$PIN"
