# RoosterControl Refactor TODO

## Step 16.1 — Extract dependency validation into Infrastructure
- [x] Create `RoosterControl/Infrastructure/Smapi/RoosterControlDependencyValidator.cs`
- [x] Modify `RoosterControl/ModEntry.cs` to use the new validator

## Step 16.2 — Extract mod-id boolean expression parsing into Helpers
- [x] Create `RoosterControl/Helpers/ModIdExpression.cs`
- [x] Modify `RoosterControl/ModEntry.cs` to use the new expression evaluator

## Step 16.3 — Extract bootstrap wiring into Infrastructure
- [ ] Create `RoosterControl/Infrastructure/Smapi/RoosterControlBootstrap.cs`
- [ ] Modify `RoosterControl/ModEntry.cs` to use the bootstrapper

## Step 16.4 — Add unit tests
- [x] Create `Tests/RoosterControl.Tests/ModIdExpressionTests.cs`

## Verification
- [x] Run `dotnet build` to verify compilation (Build succeeded. 0 Warning(s), 0 Error(s))
- [x] Run `dotnet test` to verify tests pass (Passed: 11, Failed: 0, Skipped: 0)

