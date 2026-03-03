# RoosterControl.Tests

Unit tests for RoosterControl.

## Preferences (kept intentionally)
- xUnit.
- Deterministic tests (no reliance on wall-clock time or `Game1` globals).
- Minimal mocking; when mocking is necessary, use **NSubstitute**.
- Focus on pure logic: catalogs, planning, and resolver behavior.

## Run

From `Mike_SVMods/Tests/RoosterControl.Tests`:

```bash
dotnet test
```

> Note: `Pathoschild.Stardew.ModBuildConfig` expects your local SDV + SMAPI references (same as building the mod).

## Coverage
- Fertilized egg synthesis: verifies exact synthesized IDs (<base>_Fertilized) and cloned object attributes.
