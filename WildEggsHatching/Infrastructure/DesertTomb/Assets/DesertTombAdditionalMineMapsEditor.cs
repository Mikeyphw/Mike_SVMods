using System;
using StardewModdingAPI.Events;
using WildEggsHatching.Infrastructure.Config;
using WildEggsHatching.Infrastructure.Content;
using Mikew.BirdsOfAFeather.Helpers.Content;
using WildEggsHatching.Core.Assets;

namespace WildEggsHatching.Infrastructure.DesertTomb.Assets;

/// <summary>
/// Edits the AdditionalMineMaps mod dictionary (if present) to register DesertTomb floors.
/// This keeps the prior dependency model (aedenthorn.AdditionalMineMaps) without taking a
/// compile-time reference on the mod assembly.
/// </summary>
public sealed class DesertTombAdditionalMineMapsEditor : IAssetEditor
{
    // AdditionalMineMaps asset key.
    public string AssetName => "aedenthorn.AdditionalMineMaps/dictionary";

    public AssetEditPriority Priority => AssetEditPriority.Default;

    public void Edit(IAssetData asset, ContentContext context)
    {
        if (!context.Config.DesertTombModuleEnabled)
            return;

        var mapDataType = Type.GetType("AdditionalMineMaps.MapData, AdditionalMineMaps");
        if (mapDataType is null)
            return; // AdditionalMineMaps isn't installed; nothing to edit.

        int quantity = context.Config.TombChance switch
        {
            TombChance.Low => 5,
            TombChance.Medium => 10,
            TombChance.High => 15,
            TombChance.Always => 39,
            _ => 1,
        };

        var data = asset.AsDictionary<string, object>().Data;

        // Remove old entries to keep this editor idempotent across invalidations.
        for (int i = 0; i < 50; i++)
            data.Remove($"DesertTomb_{i}");

        for (int i = 0; i < quantity; i++)
            data[$"DesertTomb_{i}"] = CreateMapData(mapDataType);
    }

    private static object CreateMapData(Type mapDataType)
    {
        var obj = Activator.CreateInstance(mapDataType)
            ?? throw new InvalidOperationException($"Failed to create {mapDataType.FullName}.");

        // Match the prior values.
        SetMember(obj, mapDataType, "forceLevel", -1);
        SetMember(obj, mapDataType, "mapPath", WeggGameAssetNames.DesertTombMap);
        SetMember(obj, mapDataType, "mapType", "M");
        SetMember(obj, mapDataType, "maxLevel", -1);
        SetMember(obj, mapDataType, "minLevel", 121);

        return obj;
    }

    private static void SetMember(object target, Type targetType, string name, object value)
    {
        var prop = targetType.GetProperty(name);
        if (prop is not null && prop.CanWrite)
        {
            prop.SetValue(target, Coerce(value, prop.PropertyType));
            return;
        }

        var field = targetType.GetField(name);
        if (field is not null)
        {
            field.SetValue(target, Coerce(value, field.FieldType));
        }
    }

    private static object? Coerce(object value, Type targetType)
    {
        if (targetType.IsInstanceOfType(value))
            return value;

        if (targetType.IsEnum && value is string s)
            return Enum.Parse(targetType, s, ignoreCase: true);

        return Convert.ChangeType(value, targetType);
    }
}
