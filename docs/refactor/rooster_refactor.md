Below is a **snapshot-tailored, diff-like, step-by-step “exact moves list”** for **`RoosterControl/ModEntry.cs`** (your current file is **~107 lines**), plus an **ultra-mechanical CUT LIST** (“anchor A → anchor B → new file/method”).

This targets the same end-state pattern you’re using elsewhere: **ModEntry = composition root**, everything else in **Infrastructure/Application/Helpers**.

---

## 0) Current `RoosterControl/ModEntry.cs` map (exact line ranges)

**File:** `RoosterControl/ModEntry.cs` (107 lines)

* Fields: `_eventRouter`, `_coordinator` **L16–17**
* Property `Config` **L20–25**
* Property `RoosterHelper` **L27**
* `GetApi()` **L29**
* `Entry(IModHelper helper)` **L31–41**
* `ValidateDependencies(IModHelper helper)` **L44–75**
* `EnsureCoordinator()` **L78–79**
* `HasModCheck(string modId)` **L81**
* `static HasMod(string modId, IModHelper helper)` **L83–106**

---

# Proposed new phase for RoosterControl PLAN.md

## Phase 16 — ModEntry Composition Root Cleanup (no behavior change)

**Goal:** shrink `ModEntry.cs` to wiring only; move dependency validation + mod-id expression parsing into dedicated files; optionally add a bootstrapper.

### Step 16.1 — Extract dependency validation into Infrastructure

**Diff (RoosterControl only)**

* `A RoosterControl/Infrastructure/Smapi/RoosterControlDependencyValidator.cs`

  * `internal static class RoosterControlDependencyValidator`
  * `internal static void ValidateOrThrow(IModHelper helper, IMonitor monitor)`
  * MOVE logic from `ModEntry.ValidateDependencies(...)`
* `M RoosterControl/ModEntry.cs`

  * Replace `ValidateDependencies(helper);` with:

    * `RoosterControlDependencyValidator.ValidateOrThrow(helper, Monitor);`
  * Delete `private void ValidateDependencies(...)` from ModEntry.

**Acceptance**

* `dotnet build` succeeds.
* Runtime behavior identical (same log messages + same throw).

---

### Step 16.2 — Extract mod-id boolean expression parsing (`&&` / `||`) into Helpers

**Diff**

* `A RoosterControl/Helpers/ModIdExpression.cs`

  * `internal static class ModIdExpression`
  * `internal static bool Evaluate(string expression, IModRegistry registry)`
  * Logic must preserve current semantics:

    * if contains `&&` → all parts must be loaded
    * else if contains `||` → any part loaded
    * else → direct `IsLoaded(expression)`
  * Keep `StringSplitOptions.TrimEntries`

* `M RoosterControl/ModEntry.cs`

  * Replace `HasModCheck(string modId)` with:

    * `public bool HasModCheck(string modId) => ModIdExpression.Evaluate(modId, Helper.ModRegistry);`
  * Replace `static HasMod(...)` with:

    * `public static bool HasMod(string modId, IModHelper helper) => ModIdExpression.Evaluate(modId, helper.ModRegistry);`
  * (Optionally delete `HasMod` entirely if nothing else calls it; keep for now for compatibility.)

**Acceptance**

* Build succeeds.
* Expression handling remains identical.

---

### Step 16.3 — Optional: Extract bootstrap wiring into Infrastructure (ModEntry becomes tiny)

**Diff**

* `A RoosterControl/Infrastructure/Smapi/RoosterControlBootstrap.cs`

  * `internal static class RoosterControlBootstrap`
  * `internal static (RoosterCoordinator coordinator, SmapiEventRouter router) Bootstrap(ModEntry mod, IModHelper helper)`
  * Contains exactly what `Entry()` currently does post-validation:

    * `saveData = new SmapiSaveDataStore(helper)`
    * `coordinator = new RoosterCoordinator(mod, saveData); coordinator.Initialize();`
    * `router = new SmapiEventRouter(helper, coordinator); router.Hook();`
* `M RoosterControl/ModEntry.cs`

  * Replace the body of `Entry(...)` with:

    * `RoosterControlDependencyValidator.ValidateOrThrow(helper, Monitor);`
    * `(_coordinator, _eventRouter) = RoosterControlBootstrap.Bootstrap(this, helper);`

**Acceptance**

* Build succeeds.
* Behavior identical (same init ordering).

---

### Step 16.4 — Optional: Split ModEntry into partials for consistent repo style

*(Purely aesthetic; safe even if file is small.)*

**Diff**

* `A RoosterControl/ModEntry.Bootstrap.cs` (contains `Entry` + `GetApi`)
* `A RoosterControl/ModEntry.Accessors.cs` (contains `Config`, `RoosterHelper`, `EnsureCoordinator`)
* `M RoosterControl/ModEntry.cs`

  * Keep only fields + class declaration.

**Acceptance**

* Build succeeds.
* No behavior changes.

---

### Step 16.5 — Optional: Tests for the extracted ModIdExpression

*(If you have a Tests subdir for RoosterControl; otherwise skip.)*

**Diff**

* `A Tests/RoosterControl.Tests/ModIdExpressionTests.cs`

  * `A&&B`, `A||B`, trimming, empty tokens behavior (match current; don’t “fix” unless you decide).

**Acceptance**

* `dotnet test` passes.

---

# Ultra-mechanical CUT LIST (anchor → anchor → destination)

## CUT 1 — Dependency validation

* **Source:** `RoosterControl/ModEntry.cs`
* **Start anchor:** `private void ValidateDependencies(IModHelper helper)`
* **End anchor:** the matching closing brace `}` for that method (current **L44–75**)
* **Destination:** `RoosterControl/Infrastructure/Smapi/RoosterControlDependencyValidator.cs`

  * `ValidateOrThrow(IModHelper helper, IMonitor monitor)`
* **Replacement in ModEntry:** replace call site inside `Entry`:

  * from: `ValidateDependencies(helper);`
  * to: `RoosterControlDependencyValidator.ValidateOrThrow(helper, Monitor);`

---

## CUT 2 — Mod-id expression evaluator (`&&` / `||`)

* **Source:** `RoosterControl/ModEntry.cs`
* **Start anchor:** `public static bool HasMod(string modId, IModHelper helper)`
* **End anchor:** the method closing brace (current **L83–106**)
* **Destination:** `RoosterControl/Helpers/ModIdExpression.cs`

  * method: `Evaluate(string expression, IModRegistry registry)`
* **Replacement in ModEntry:**

  * `public static bool HasMod(string modId, IModHelper helper) => ModIdExpression.Evaluate(modId, helper.ModRegistry);`

---

## CUT 3 — Instance wrapper for HasMod (optional, keep for compatibility)

* **Source:** `RoosterControl/ModEntry.cs`
* **Start anchor:** `public bool HasModCheck(string modId) =>`
* **End anchor:** same line (current **L81**)
* **Destination:** keep in `ModEntry.cs` OR move to `ModEntry.Accessors.cs`
* **Replacement:** `=> ModIdExpression.Evaluate(modId, Helper.ModRegistry);`

---

## CUT 4 — Bootstrap coordinator/router wiring (optional)

* **Source:** `RoosterControl/ModEntry.cs`
* **Start anchor:** `public override void Entry(IModHelper helper)`
* **End anchor:** closing brace of Entry (current **L31–41**)
* **Destination:** `RoosterControl/Infrastructure/Smapi/RoosterControlBootstrap.cs`

  * method: `Bootstrap(ModEntry mod, IModHelper helper)`
* **Replacement in ModEntry.Entry:** 2 lines:

  * validate
  * assign tuple return to `_coordinator`, `_eventRouter`

---

## CUT 5 — Accessors partial split (optional)

* **Source anchors:**

  * `internal RoosterControlConfig Config` block (**L20–25**)
  * `internal RoosterHelper RoosterHelper => ...` (**L27**)
  * `private RoosterCoordinator EnsureCoordinator()` (**L78–79**)
* **Destination:** `RoosterControl/ModEntry.Accessors.cs` (partial class)
* **Replacement in ModEntry.cs:** remove those members from the main file.

---

# End-state: what `ModEntry.cs` looks like (conceptually)

After 16.1–16.3, ModEntry becomes essentially:

* fields
* accessors
* `GetApi`
* `Entry` calling validator + bootstrapper

---
