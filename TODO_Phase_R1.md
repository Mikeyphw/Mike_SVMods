# Phase R1 Completion Plan

## Current Status
The following changes have been made (uncommitted):
- ✅ Phase R1B: LetMeHatch GMCM ids and i18n keys normalized
- ✅ Phase R1C: RoosterControl legacy GMCM i18n aliases pruned
- ❌ Phase R1D: EggsInWild verification not yet run
- ❌ Phase R1E: Final validation not yet done
- ❌ Build gate not yet passed

## Remaining Steps

### 1. Run R1D Verification (EggsInWild)
```bash
# Verify no legacy i18n keys
rg -n 'config\.(Bool|Number|Text|Page|Section)\.' EggsInWild

# Verify no PascalCase option ids
rg -n 'optionId:\s*"[A-Z]' EggsInWild/Infrastructure/Integrations/Gmcm EggsInWild/ModEntry.Gmcm.cs

# Verify no fake-bool buttons
rg -n 'getValue:\s*\(\)\s*=>\s*false' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs'

# Verify Action usage exists
rg -n '\.Action\(' EggsInWild/Infrastructure/Integrations/Gmcm -g'*.cs' | wc -l
```

### 2. Run Build Gate
```bash
dotnet build NestCore/NestCore.csproj
dotnet build RoosterControl/RoosterControl.csproj
dotnet build LetMeHatch/LetMeHatch.csproj
dotnet build EggsInWild/EggsInWild.csproj
dotnet build Mike_SVMods.sln
```

### 3. Update Docs (CHECKLIST.md, TODO.md)
Add Phase R1B and R1C completion notes, plus R1D verification notes.

### 4. Commit Each Phase
- Commit R1B: Normalize LetMeHatch GMCM ids and i18n keys
- Commit R1C: Prune RoosterControl legacy GMCM i18n aliases  
- Commit R1D: Verify EggsInWild GMCM ids already normalized

### 5. Run R1E Final Validation
Run all gates from Phase R1E to ensure everything is complete.

### 6. Finalize Docs and Commit
- Commit R1E: Finalize repo GMCM id/i18n normalization

