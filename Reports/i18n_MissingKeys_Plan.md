# i18n Keys Without Entries - Fix Plan

## Summary
This document outlines the missing i18n keys found across the Mike_SVMods project and provides a plan to fix them.

---

## Missing Keys

### 1. LetMeHatch/i18n/default.json

| Key | Status | Priority |
|-----|--------|----------|
| `config.notavaliable` | Missing (exists in pt-BR.json) | HIGH |
| `config.EnableMale.description` | Empty string | MEDIUM |
| `config.MaleChance.description` | Empty string | MEDIUM |
| `config.colors.paragraph` | Missing (exists in pt-BR.json) | HIGH |

### 2. EggsInWild/i18n/default.json

| Key | Status | Priority |
|-----|--------|----------|
| `specialOrder.WildEggIncubatorII.objective.0.message_seed` | Missing | HIGH |
| `specialOrder.WildEggIncubatorII.objective.0.message_fruit` | Missing | HIGH |
| `specialOrder.WildEggIncubatorII.objective.0.text_seed` | Missing | HIGH |
| `specialOrder.WildEggIncubatorII.objective.0.text_fruit` | Missing | HIGH |

---

## Fix Plan

### Step 1: Fix LetMeHatch/i18n/default.json

Add the following missing keys:

```json
{
  "config.notavaliable": "Not available yet. Load a save first.",
  "config.colors.paragraph": "Configure egg coloring. Palettes control which color families are allowed when LMH randomizes colors; overrides let you pick a fixed color per egg item."
}
```

Optionally improve empty descriptions:
```json
{
  "config.EnableMale.description": "Enable this male option for breeding.",
  "config.MaleChance.description": "Chance to select this male."
}
```

### Step 2: Fix EggsInWild/i18n/default.json

Add the missing specialOrder keys based on the existing pattern in `SpecialOrdersEditor.cs`:

```json
{
  "specialOrder.WildEggIncubatorII.objective.0.message_seed": "Ancient Seed",
  "specialOrder.WildEggIncubatorII.objective.0.message_fruit": "Ancient Fruit",
  "specialOrder.WildEggIncubatorII.objective.0.text_seed": "Bring me an ancient seed.",
  "specialOrder.WildEggIncubatorII.objective.0.text_fruit": "Bring me an ancient fruit."
}
```

---

## Verification

After fixes, verify:
1. No runtime errors from missing translation keys
2. GMCM displays all labels correctly
3. Special orders show correct objectives

---

## Notes

- RoosterControl/i18n/default.json - All GMCM "allowed values" keys are present
- NestCore/i18n/default.json - No missing keys detected
- pt-BR.json (LetMeHatch) has translations that could be backported to default.json

