# INTERACTION CONTRACT

This document defines the intended runtime relationship between the mods in this repository:

- `Mikew.nestcore` (`NestCore`)
- `Mikew.lmh` (`LetMeHatch`)
- `Mikew.roostersaddon.smapi` (`RoosterControl`)
- `Mikew.eggsinwild` (`EggsInWild`)

The goal is simple: one authority per domain, typed APIs for cross-mod reads, and no hidden cross-mod writes.

## 1. Domain ownership

| Domain | Primary authority | Other mods may | Other mods must not |
|---|---|---|---|
| Shared UI/integration helpers | `NestCore` | consume helpers and builders through `INestCoreApi` | reimplement NestCore-owned contract surfaces under the same semantics |
| Clone egg generation and clone animal definitions | `LetMeHatch` | read clone info through `ILetMeHatchApi` | mutate LMH-owned clone rules from another mod's private state |
| Rooster-specific policy, hatching choices, rooster UI | `RoosterControl` | consume rooster state through explicit API only | directly infer or overwrite rooster policy by patching RC internals |
| Wild egg progression, shop gating, wild egg world logic, Desert Tomb logic | `EggsInWild` | read stable state through `IWildEggsApi` | write progression state or bypass EIW-owned gating logic |

## 2. Integration direction

Allowed dependency direction:

- `NestCore` -> no dependency on the other three mods
- `LetMeHatch` -> may depend on `NestCore`, may read `EggsInWild`, may read `RoosterControl`
- `RoosterControl` -> may depend on `NestCore`, may read `LetMeHatch`
- `EggsInWild` -> may depend on `NestCore`, may read `LetMeHatch`

Avoid adding reverse dependencies unless there is no stable alternative. In particular:

- `NestCore` must stay generic and not take gameplay dependencies on `LetMeHatch`, `RoosterControl`, or `EggsInWild`.
- `RoosterControl` and `EggsInWild` should not become mutually authoritative over the same gameplay rule.

## 3. Asset ownership

These mods all touch overlapping game data. Ownership must stay explicit.

| Asset | Primary writer | Secondary writers allowed | Rule |
|---|---|---|---|
| `Data/FarmAnimals` | `LetMeHatch` for clone/hatching structure; `RoosterControl` for rooster policy; `EggsInWild` for progression/shop gating | yes, but each edit must stay in its own field subset | No mod may rewrite another mod's conceptual ownership area. Keep edits additive and field-scoped. |
| `Data/Objects` | `LetMeHatch` for cloned eggs and fertilized eggs; `RoosterControl` for rooster-facing object edits; `EggsInWild` for wild egg items | yes | If two mods need the same object ID, the design is wrong. IDs must be unique and mod-owned. |
| `Data/Machines` | `LetMeHatch` for hatch/incubator behavior; `EggsInWild` only for wild-egg-specific machine interactions | yes, if the edits are disjoint | Do not use broad replacement logic that assumes sole ownership of a machine entry. |
| mod-private `Mods/<UniqueId>/...` assets | owning mod only | no | These are private by default unless documented as public contract assets. |

### Field ownership inside `Data/FarmAnimals`

Use this working rule when introducing or reviewing edits:

- `LetMeHatch` owns clone animal creation, clone egg references, breeding-related fields, and clone placeholder/birth-text support.
- `RoosterControl` owns rooster role semantics, rooster source selection, and rooster-specific hatch plan application.
- `EggsInWild` owns progression gating, note/shop unlock relationships, and wild-animal progression overlays or annotations.

If a new feature needs the same field to be changed by two mods, stop and define an explicit merge contract first.

## 4. Cross-mod API rules

### Public APIs currently in use

| Provider | API | Current version signal | Intent |
|---|---|---|---|
| `NestCore` | `INestCoreApi` | `ApiVersion = 2` | Shared GMCM/StardewUI/integration helpers |
| `EggsInWild` | `IWildEggsApi` | `ApiVersion = 1` | Query stable Wild Eggs state |
| `LetMeHatch` | `ILetMeHatchApi` | none | Query clone animal/egg state |
| `RoosterControl` | `IRoosterHatchingAPI` | none | Query rooster hatching state |

### Rules

1. Prefer typed API access over reflection, content sniffing, or compatibility shims.
2. If an API exposes `ApiVersion`, check it before relying on version-sensitive behavior.
3. If an API does not expose a version yet, keep the consuming code narrow and null-safe.
4. A mod may read another mod's API state, but should not treat setter methods as a general-purpose mutation channel unless the provider explicitly intends that API to be authoritative input.
5. New cross-mod behavior should be added to a typed API before adding fallback parsing logic.

### Versioning requirement for future work

Any new public API in this repo should include:

- `int ApiVersion { get; }`
- a short comment describing whether the surface is read-only, write-capable, or mixed
- one documented failure mode when the provider is installed but not ready

`LetMeHatch` and `RoosterControl` should add version properties to their public APIs when those surfaces next change.

## 5. Missing integration behavior

Optional integrations must fail soft.

| Situation | Required behavior |
|---|---|
| Optional mod not installed | Skip the integration path, log once at `Info` or `Warn`, continue running |
| Optional mod installed but API unavailable | Treat as not ready, skip dependent behavior, avoid repeated log spam |
| API version mismatch | Disable only the incompatible integration path, log concrete provider and version values |
| Shared asset not yet loadable | Defer or rebuild later; do not cache a permanent failure during startup |

Concrete expectation:

- `LetMeHatch` may continue without `RoosterControl`.
- `LetMeHatch` already treats `EggsInWild` as important for early init; if that remains required, the log should say that explicitly instead of looking like a generic missing-API failure.
- `EggsInWild` should continue when `LetMeHatch` is absent, but any LMH-dependent preview or validation path must degrade to a no-op or explanatory warning.
- `RoosterControl` should continue when `LetMeHatch` is absent and simply disable LMH-specific compat features.

## 6. Messaging and shared identifiers

Rules for custom message and shared-key usage:

1. Message types must be namespaced per mod and defined in one place.
2. Shared `modData` keys, save keys, and asset prefixes must be constants, not ad hoc strings copied across mods.
3. If a second mod needs to understand a key owned by another mod, the key belongs in a public contract namespace or API doc.

Current examples that already follow the intent:

- `EggsInWild` routes its own multiplayer messages through `ModManifest.UniqueID`.
- `NestCore` and the other mods already use stable `Mods/<UniqueId>/...` asset prefixes.

## 7. Diagnostics contract

Each mod should be able to answer these questions in one command or log block:

- Which optional integrations were detected?
- Which APIs were acquired?
- Which API versions were observed?
- Which asset editors/loaders are active?
- Which integration paths are disabled and why?

Minimum expectation by mod:

- `NestCore`: report API availability for GMCM, GMCM Options, and StardewUI.
- `LetMeHatch`: report `EggsInWild` API state, `RoosterControl` API state, and early-init readiness.
- `RoosterControl`: report `LetMeHatch` API state and active compat source catalogs.
- `EggsInWild`: report `NestCore` API state, `LetMeHatch` API state, progression state, and Desert Tomb module state.

## 8. Change-control rules

Before merging a cross-mod feature:

1. Identify the single authority mod.
2. Identify whether the feature needs reads only or reads plus writes.
3. If writes are needed across mod boundaries, add an explicit API or documented contract first.
4. Document any new shared asset ownership or field ownership rule in this file.
5. Verify the absent-mod path still behaves correctly.

## 9. Default decisions for this repo

Use these defaults unless there is a documented exception:

- Cross-mod reads: allowed through typed APIs.
- Cross-mod writes: disallowed unless the provider exposes an explicit API for it.
- Asset edits: additive and field-scoped, never whole-entry replacement when another mod may also edit the asset.
- Private implementation details: not contractually stable.
- `NestCore`: the only place shared UI/helper infrastructure should be generalized.

## 10. Immediate follow-up candidates

These are not requirements for correctness, but they would strengthen the contract:

1. Add `ApiVersion` to `ILetMeHatchApi`.
2. Add `ApiVersion` to `IRoosterHatchingAPI`.
3. Add one status/diagnostics command per mod that prints integration readiness in a consistent format.
4. Centralize shared cross-mod string constants where they are currently duplicated.
