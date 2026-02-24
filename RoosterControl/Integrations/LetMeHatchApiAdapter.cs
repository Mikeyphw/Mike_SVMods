using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mikew.NestCore.Framework;

namespace RoosterControl.Integrations;

/// <summary>
/// Reflection-based adapter to support older LetMeHatch API implementations while enforcing the NestCore contract.
/// </summary>
internal sealed class LetMeHatchApiAdapter : ILetMeHatchApi
{
    private readonly object _api;
    private readonly PropertyInfo? _animalsWithClonedEggs;
    private readonly PropertyInfo? _finishedEarlyStuff;
    private readonly PropertyInfo? _editBirthTextPriority;
    private readonly PropertyInfo? _editEggListPriority;
    private readonly PropertyInfo? _clonedEggsPriority;
    private readonly PropertyInfo? _newEggsAddPriority;
    private readonly MethodInfo? _getClonedAnimalsInfo;

    private LetMeHatchApiAdapter(object api)
    {
        _api = api;
        var t = api.GetType();
        _animalsWithClonedEggs = t.GetProperty(nameof(AnimalsWithClonedEggs), BindingFlags.Instance | BindingFlags.Public);
        _finishedEarlyStuff = t.GetProperty(nameof(FinishedEarlyStuff), BindingFlags.Instance | BindingFlags.Public);
        _editBirthTextPriority = t.GetProperty(nameof(EDIT_BIRTH_TEXT_PRIORITY), BindingFlags.Instance | BindingFlags.Public);
        _editEggListPriority = t.GetProperty(nameof(EDIT_EGG_LIST_PRIORITY), BindingFlags.Instance | BindingFlags.Public);
        _clonedEggsPriority = t.GetProperty(nameof(CLONED_EGGS_PRIORITY), BindingFlags.Instance | BindingFlags.Public);
        _newEggsAddPriority = t.GetProperty(nameof(NEW_EGGS_ADD_PRIORITY), BindingFlags.Instance | BindingFlags.Public);
        _getClonedAnimalsInfo = t.GetMethod(nameof(GetClonedAnimalsInfo), BindingFlags.Instance | BindingFlags.Public);
    }

    public static ILetMeHatchApi? Wrap(object? api)
    {
        if (api is null)
            return null;

        if (api is ILetMeHatchApi typed)
            return typed;

        return new LetMeHatchApiAdapter(api);
    }

    public List<string> AnimalsWithClonedEggs =>
        _animalsWithClonedEggs?.GetValue(_api) as List<string> ?? new();

    public bool FinishedEarlyStuff =>
        _finishedEarlyStuff?.GetValue(_api) is bool b && b;

    public int EDIT_BIRTH_TEXT_PRIORITY =>
        _editBirthTextPriority?.GetValue(_api) is int v ? v : 0;

    public int EDIT_EGG_LIST_PRIORITY =>
        _editEggListPriority?.GetValue(_api) is int v ? v : 0;

    public int CLONED_EGGS_PRIORITY =>
        _clonedEggsPriority?.GetValue(_api) is int v ? v : 0;

    public int NEW_EGGS_ADD_PRIORITY =>
        _newEggsAddPriority?.GetValue(_api) is int v ? v : 0;

    public List<(string FemaleKey, string MaleKey, bool AddAsAlternatePurchase)> GetClonedAnimalsInfo()
    {
        if (_getClonedAnimalsInfo is null)
            return new();

        var raw = _getClonedAnimalsInfo.Invoke(_api, Array.Empty<object>());
        if (raw is null)
            return new();

        if (raw is List<(string FemaleKey, string MaleKey, bool AddAsAlternatePurchase)> typed)
            return typed;

        if (raw is IEnumerable enumerable)
        {
            var result = new List<(string FemaleKey, string MaleKey, bool AddAsAlternatePurchase)>();
            foreach (var item in enumerable)
            {
                if (item is ValueTuple<string, string, bool> vt)
                {
                    result.Add((vt.Item1, vt.Item2, vt.Item3));
                    continue;
                }

                var it = item?.GetType();
                if (it is null)
                    continue;

                // best-effort for anonymous tuples
                var f = it.GetField("Item1");
                var m = it.GetField("Item2");
                var a = it.GetField("Item3");
                if (f?.GetValue(item) is string fs && m?.GetValue(item) is string ms && a?.GetValue(item) is bool ab)
                    result.Add((fs, ms, ab));
            }
            return result;
        }

        return new();
    }
}
