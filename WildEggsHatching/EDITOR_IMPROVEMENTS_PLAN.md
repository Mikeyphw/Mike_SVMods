# Editor Improvements Plan

## Overview
Add internationalization (i18n) support and refactor common patterns into the base class.

## Common Patterns Found in Original Editors

These patterns can be extracted to `ProgressionAwareAssetEditor.cs`:

### 1. ObjectData Factory Helper
```csharp
// ObjectEditor.cs - CreateObjectData()
private ObjectData CreateObjectData(
    string description, string displayName, string name,
    string texturePath, int spriteIndex, int price,
    List<string>? contextTags, Dictionary<string,string>? customFields)
```
- Creates ObjectData with i18n support
- Uses AssetHandler for texture paths
- Used by: ObjectEditor (CreateJunimoNote, CreateOsirisScroll, CreateElixir, CreateWildEgg)

### 2. BigCraftableData Factory Helper
```csharp
// BigCraftableEditor.cs - CreateWildEggIncubator()
private BigCraftableData CreateBigCraftable(
    string name, string displayName, string description,
    string texturePath, int spriteIndex, bool canBePlacedIndoors)
```
- Creates BigCraftableData with i18n
- Used by: BigCraftableEditor

### 3. MachineData Factory Helper
```csharp
// MachineEditor.cs - CreateMachineData()
private MachineData CreateMachineData(
    List<MachineOutputRule> outputRules,
    List<MachineItemAdditionalConsumedItems>? consumedItems,
    string? invalidItemMessage, bool isIncubator)
```
- Creates complex MachineData with output rules, effects

### 4. Recipe String Factory
```csharp
// CraftingRecipeEditor.cs - CreateCraftingRecipeData()
private string CreateRecipeData(
    List<(string itemID, int quantity)> ingredients,
    (string outputID, int quantity) output,
    bool isBigCraftable, string? unlockConditions)
```
- Creates crafting recipe strings in SMAPI format

### 5. Runtime Config Modification Methods
- `EditWildEgg()` - modifies existing Data/Objects entry
- `EditOtherIncubators()` - modifies existing machine entries  
- `EditWildEggIncubatorRules()` - conditional rule additions
- `EditCraftingRecipesAsset()` - uses DataFieldsEditor.ModifyData()

### 6. Image Patching
```csharp
// BigCraftableEditor.cs - EditWildEggIncubatorAsset()
void EditWildEggIncubatorAsset(IAssetDataForImage editor)
```
- Uses IRawTextureData, PatchImage, source/target rectangles

### 7. Dictionary Extensions
```csharp
data.TryAddMany(ItemsToAdd);
```
- Populates dictionary if empty

## Implementation: Add to ProgressionAwareAssetEditor.cs

### New Protected Methods to Add:
1. `CreateObjectData()` - ObjectData factory with i18n and texture helpers
2. `CreateBigCraftableData()` - BigCraftableData factory  
3. `CreateMachineData()` - MachineData with output rules builder
4. `CreateRecipeString()` - Recipe string builder
5. `PatchImage()` - Image patching helper for big craftables
6. `GetModTexturePath()` - AssetHandler wrapper for texture paths
7. `ModifyRecipeField()` - DataFieldsEditor wrapper for runtime config

### New Protected Properties:
- `I18n` - Translation helper (from BirdsOfAFeather)
- `AssetHandler` - ModAssetHandler for paths

### Already Covered in Refactor Folder:
| Feature | Location in Refactor |
|---------|---------------------|
| Note spawning for animals | `refactor/Notes/NoteService.cs` |
| Animal tracking/hatching | `refactor/Tracking/AnimalTrackingService.cs` |
| Basic progression mode edits | `FarmAnimalsEditor.ApplyProgressionModeEdits()` |
| VanillaAnimals config mapping | `FarmAnimalsEditor.VanillaAnimalPrices` dictionary |

## Remaining: Editor Features to Add

### ObjectEditor.cs - Missing
1. **JunimoNote Creation** - Creates ObjectData for hatched animals
2. **EditWildEgg()** - Modifies FarmWEgg (base game egg)
3. **ChangeElixirSprite()** - Runtime sprite switching
4. **Prismatic Valley Framework** - Custom fields for color palettes

### MachineEditor.cs - Missing
1. **AddMachineOutputRulesForEggs()** - CP mod compatibility (Cheese Press, Mayonnaise Machine)
2. **EditOtherIncubators()** - Modifies vanilla incubators to reject wild eggs
3. **EditWildEggIncubatorRules()** - Runtime config (HatchDinoEggs, NeedsFern)

### BigCraftableEditor.cs - Missing
1. **EditWildEggIncubatorAsset()** - Sprite image patching

### CraftingRecipesEditor.cs - Missing  
1. **EditCraftingRecipesAsset()** - Runtime config (ProgressionMode, NeedsFern, FruitInsteadOfSeed)
2. **Elixir Recipe Creation** - Different ingredients from incubator

### SpecialOrderEditor.cs - Missing
1. **SetUpDropBox()** - Forest tile property setup

### MailEditor.cs - Missing
1. **Dynamic Note Mail Creation** - For hatched animals

## Implementation Phases

1. **Phase 1: Add Base Class Helpers**
   - Add I18n, AssetHandler properties to ProgressionAwareAssetEditor
   - Add factory methods: CreateObjectData, CreateBigCraftableData, CreateMachineData, CreateRecipeString
   - Add ModifyRecipeField() wrapper

2. **Phase 2: Fix FarmAnimalsEditor**
   - Already has dictionary mapping - clean up reflection issue if any

3. **Phase 3: Add Missing Editor Features**
   - ObjectEditor: JunimoNote, EditWildEgg, ChangeElixirSprite
   - CraftingRecipesEditor: EditCraftingRecipesAsset, Elixir recipe
   - BigCraftableEditor: EditWildEggIncubatorAsset

4. **Phase 4: Integration Features**
   - MachineEditor: AddMachineOutputRulesForEggs, EditOtherIncubators
   - SpecialOrderEditor: SetUpDropBox
   - MailEditor: Dynamic note mail

## Files to Modify

1. `refactor/Infrastructure/Content/ProgressionAwareAssetEditor.cs` - **Add common helpers**
2. `refactor/Infrastructure/Content/WildEggConstants.cs` - Add i18n keys, fix VanillaItems
3. `refactor/Infrastructure/Content/Editors/CraftingRecipesEditor.cs` - Use base class helpers, add EditCraftingRecipesAsset
4. `refactor/Infrastructure/Content/Editors/FarmAnimalsEditor.cs` - Verify dictionary usage
5. `refactor/Infrastructure/Content/Editors/MachinesEditor.cs` - Add missing methods
6. `refactor/Infrastructure/Content/Editors/ObjectsEditor.cs` - Use base class helpers
7. `refactor/Infrastructure/Content/Editors/MailEditor.cs` - Add dynamic note mail
8. `refactor/Infrastructure/Content/Editors/SpecialOrdersEditor.cs` - Add SetUpDropBox
9. `refactor/Infrastructure/Content/Editors/BigCraftableEditor.cs` - Add sprite patching
10. Create `refactor/i18n/default.json`

## Dependencies
- BirdsOfAFeather.dll reference
- BirdsOfAFeather.DataFieldsEditor for recipe modifications
- BirdsOfAFeather.ModAssetHandler for asset paths

