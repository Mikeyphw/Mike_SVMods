# Phase R1D — EggsInWild strict verification table (snapshot: 2026-03-05)

## Purpose
Prove EggsInWild is already GMCM-id/i18n-normalized in the current repo state.
This phase is **docs-only** if (and only if) all gates below pass.

---

## Allowed files to edit (ONLY if all gates pass)
- docs/PLAN.md
- docs/CHECKLIST.md
- docs/TODO.md

## Forbidden changes
- Do NOT edit any EggsInWild `.cs` files
- Do NOT edit `EggsInWild/i18n/default.json`
- Do NOT create `EggsInWild/i18n/pt-br.json` (it does not exist in this snapshot)

---

## Gate 0 — Confirm expected files exist / do not exist

### Bash
- Must exist:
  - `EggsInWild/i18n/default.json`
- Must NOT exist:
  - `EggsInWild/i18n/pt-br.json`
  - `EggsInWild/ModEntry.Gmcm.WildAnimals.cs`

Commands:
```bash
test -f EggsInWild/i18n/default.json
test ! -f EggsInWild/i18n/pt-br.json
test ! -f EggsInWild/ModEntry.Gmcm.WildAnimals.cs
````

### PowerShell

```powershell
Test-Path EggsInWild/i18n/default.json | Should -BeTrue
Test-Path EggsInWild/i18n/pt-br.json | Should -BeFalse
Test-Path EggsInWild/ModEntry.Gmcm.WildAnimals.cs | Should -BeFalse
```

If any of these checks fail: STOP.

---

## Gate 1 — No legacy GMCM i18n alias prefixes anywhere in EggsInWild

Expected: **0 matches**

### Bash / PowerShell (same)

```bash
rg -n 'config\.(Bool|Number|Text|Page|Section)\.' EggsInWild
```

If any match: STOP.

---

## Gate 2 — No PascalCase GMCM option ids in EggsInWild GMCM code

Expected: **0 matches**

```bash
rg -n 'optionId:\s*"[A-Z]' EggsInWild/Infrastructure/Integrations/Gmcm EggsInWild/ModEntry.Gmcm.cs
```

If any match: STOP.

---

## Gate 3 — No PascalCase GMCM page ids in EggsInWild GMCM code

Expected: **0 matches**

```bash
rg -n '\b(Page|Link|Links)\(\s*"[A-Z]' EggsInWild/Infrastructure/Integrations/Gmcm EggsInWild/ModEntry.Gmcm.cs
```

If any match: STOP.

---

## Gate 4 — No fake-bool "button" patterns remain

Expected: **0 matches**

```bash
rg -n 'getValue:\s*\(\)\s*=>\s*false' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs'
```

If any match: STOP.

---

## Gate 5 — One-shot commands use Action(...) (sanity)

Expected: **>= 1 match**
Snapshot expectation: **exactly 5 matches**

### Bash

```bash
rg -n '\.Action\(' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs' | wc -l
```

### PowerShell

```powershell
(rg -n '\.Action\(' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs').Count
```

If result is 0: STOP.

---

## Gate 6 — Snapshot count sanity checks (must match EXACTLY for this snapshot)

These are "drift detectors". If they don't match, STOP and report (do not edit anything).

### Expected counts

* `optionId:` occurrences = **27**
* `Page("...")` occurrences = **7**
* `Link("...")` occurrences = **9**
* `.Action(` occurrences = **5**

### Bash

```bash
rg -n 'optionId:\s*"' EggsInWild/Infrastructure/Integrations/Gmcm EggsInWild/ModEntry.Gmcm.cs -g'*.cs' | wc -l
rg -n '\bPage\(\s*"' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs' | wc -l
rg -n '\bLink\(\s*"' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs' | wc -l
rg -n '\.Action\(' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs' | wc -l
```

### PowerShell

```powershell
(rg -n 'optionId:\s*"' EggsInWild/Infrastructure/Integrations/Gmcm EggsInWild/ModEntry.Gmcm.cs -g'*.cs').Count
(rg -n '\bPage\(\s*"' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs').Count
(rg -n '\bLink\(\s*"' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs').Count
(rg -n '\.Action\(' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs').Count
```

If any count differs: STOP and report the deltas.

---

## Gate 7 — Confirm expected top-level GMCM page id set (informational, but must match)

Expected page ids set:

* `animalNotes`
* `animalProgression`
* `debug`
* `desertTomb`
* `hatching`
* `items`
* `prices`
* `wildAnimals`

Command (must show ONLY these 8 unique ids):

```bash
rg -n '\bPage\(\s*"[^"]+"' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs' \
  | rg -o '"[^"]+"' \
  | tr -d '"' \
  | sort -u
```

If any extra/missing id appears: STOP.

---

# If ALL gates pass → docs-only updates (exact insert text)

## 1) docs/PLAN.md

In the `## Status` bullet list, append this bullet as the last item:

* `- Repo Phase R1D: Verified EggsInWild GMCM ids and i18n already match repo normalization rules (no code changes required).`

## 2) docs/CHECKLIST.md

Append this section at the end of the file:

## Repo Phase R1D — GMCM id/i18n verification (no code changes)

* [x] Verified no legacy i18n alias keys `config.(Bool|Number|Text|Page|Section).*` exist under EggsInWild.
* [x] Verified no PascalCase GMCM ids exist in `optionId:` or `Page/Link` strings.
* [x] Verified one-shot commands use `Action(...)` (no fake-bool buttons).
* [x] Verified `EggsInWild/ModEntry.Gmcm.WildAnimals.cs` does not exist in this snapshot.
* [ ] Verified build.

## 3) docs/TODO.md

Append this section at the end of the file:

## Repo maintenance

* [x] Repo Phase R1D: EggsInWild GMCM ids/i18n verified already normalized (docs-only).

---

# Build gate (required even for docs-only)

```bash
dotnet build EggsInWild/EggsInWild.csproj
dotnet build Mike_SVMods.sln
```

After successful build, in `docs/CHECKLIST.md` tick:

* `[x] Verified build.`

---

# Commit (docs-only)

```bash
git add docs/PLAN.md docs/CHECKLIST.md docs/TODO.md
git commit -m "phase-R1d: verify EggsInWild GMCM ids already normalized"
```
