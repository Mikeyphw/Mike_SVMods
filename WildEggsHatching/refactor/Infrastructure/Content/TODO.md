# Infrastructure/Content Refactor TODO

## Issues to Fix:

### 1. IAssetEditor.cs
- [ ] Simplify interface - remove progression logic from contract
- [ ] Make ShouldApply() simpler

### 2. ProgressionAwareAssetEditor.cs
- [ ] Remove direct PhaseProgressionService reference
- [ ] Remove direct IConfigProvider reference  
- [ ] Move progression logic out of base class
- [ ] Accept pre-computed current step as parameter

### 3. SmapiContentEditor.cs
- [ ] Remove direct ModEntry references
- [ ] Accept dependencies through constructor

### 4. SmapiContentCoordinator.cs
- [ ] Remove reflection-based Edit calls
- [ ] Remove ModEntry references
- [ ] Accept only current step (not full service)

### 5. Editors (ObjectsEditor, etc.)
- [ ] Refactor to accept current step from outside
- [ ] Remove direct ConfigProvider usage in Edit methods

