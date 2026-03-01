using System;
using System.Collections.Generic;
using System.Text.Json;

namespace RoosterControl.Application.Produces;

/// <summary>
/// Parser + validator for the Phase 8 produce override rules JSON.
/// This is a pure component: it does not depend on SMAPI/game types.
/// </summary>
public static class ProduceOverrideRules
{
    /// <summary>
    /// Parse and validate a rules JSON payload.
    /// The payload may be either a root array <c>[...]</c> or an object with a <c>rules</c> array property.
    /// </summary>
    public static ProduceOverrideParseResult Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return ProduceOverrideParseResult.ValidEmpty;

        var issues = new List<ProduceOverrideIssue>();
        var rules = new List<ProduceOverrideRule>();

        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (Exception ex)
        {
            issues.Add(
                new ProduceOverrideIssue(
                    Severity: ProduceOverrideIssueSeverity.Error,
                    Code: "invalid_json",
                    Message: $"Rules JSON could not be parsed: {ex.Message}",
                    JsonPath: "$"
                )
            );

            return new ProduceOverrideParseResult(new ProduceOverrideRuleSet(Array.Empty<ProduceOverrideRule>()), issues);
        }

        using (doc)
        {
            var root = doc.RootElement;
            JsonElement rulesArray;

            if (root.ValueKind == JsonValueKind.Array)
            {
                rulesArray = root;
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetPropertyIgnoreCase(root, "rules", out var r) && r.ValueKind == JsonValueKind.Array)
                {
                    rulesArray = r;
                }
                else
                {
                    issues.Add(
                        new ProduceOverrideIssue(
                            ProduceOverrideIssueSeverity.Error,
                            "missing_rules_array",
                            "Rules JSON must be a root array or an object containing a 'rules' array.",
                            "$"
                        )
                    );

                    return new ProduceOverrideParseResult(new ProduceOverrideRuleSet(Array.Empty<ProduceOverrideRule>()), issues);
                }
            }
            else
            {
                issues.Add(
                    new ProduceOverrideIssue(
                        ProduceOverrideIssueSeverity.Error,
                        "invalid_root",
                        "Rules JSON must be a root array or an object containing a 'rules' array.",
                        "$"
                    )
                );

                return new ProduceOverrideParseResult(new ProduceOverrideRuleSet(Array.Empty<ProduceOverrideRule>()), issues);
            }

            var index = 0;
            foreach (var ruleEl in rulesArray.EnumerateArray())
            {
                var rulePath = $"$[{index}]";

                if (ruleEl.ValueKind != JsonValueKind.Object)
                {
                    issues.Add(
                        new ProduceOverrideIssue(
                            ProduceOverrideIssueSeverity.Error,
                            "invalid_rule",
                            "Each rule must be a JSON object.",
                            rulePath
                        )
                    );
                    index++;
                    continue;
                }

                var ruleObj = ruleEl;

                var id = ReadOptionalString(ruleObj, "id", issues, $"{rulePath}.id")?.Trim();
                if (id is not null && id.Length == 0)
                {
                    issues.Add(
                        new ProduceOverrideIssue(
                            ProduceOverrideIssueSeverity.Error,
                            "invalid_id",
                            "Rule 'id' must be a non-empty string when provided.",
                            $"{rulePath}.id"
                        )
                    );
                    id = null;
                }

                var when = ParseWhen(ruleObj, issues, rulePath);
                var then = ParseThen(ruleObj, issues, rulePath);

                // Require at least one action.
                if (then is not null && then.IsEmpty)
                {
                    issues.Add(
                        new ProduceOverrideIssue(
                            ProduceOverrideIssueSeverity.Error,
                            "empty_then",
                            "Rule 'then' must contain at least one action.",
                            $"{rulePath}.then"
                        )
                    );
                }

                WarnUnknownProperties(ruleObj, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "id", "when", "then" }, issues, rulePath);

                if (then is not null)
                {
                    rules.Add(
                        new ProduceOverrideRule(
                            OrderIndex: index,
                            Id: id,
                            When: when ?? new ProduceOverrideWhen(),
                            Then: then
                        )
                    );
                }

                index++;
            }
        }

        return new ProduceOverrideParseResult(new ProduceOverrideRuleSet(rules), issues);
    }

    private static ProduceOverrideWhen? ParseWhen(JsonElement ruleObj, List<ProduceOverrideIssue> issues, string rulePath)
    {
        if (!TryGetPropertyIgnoreCase(ruleObj, "when", out var whenEl))
            return new ProduceOverrideWhen();

        var whenPath = $"{rulePath}.when";

        if (whenEl.ValueKind == JsonValueKind.Null)
            return new ProduceOverrideWhen();

        if (whenEl.ValueKind != JsonValueKind.Object)
        {
            issues.Add(
                new ProduceOverrideIssue(
                    ProduceOverrideIssueSeverity.Error,
                    "invalid_when",
                    "Rule 'when' must be an object when present.",
                    whenPath
                )
            );
            return null;
        }

        var roosterType = ReadOptionalString(whenEl, "roosterType", issues, $"{whenPath}.roosterType")?.Trim();
        var buildingType = ReadOptionalString(whenEl, "buildingType", issues, $"{whenPath}.buildingType")?.Trim();
        var locationName = ReadOptionalString(whenEl, "locationName", issues, $"{whenPath}.locationName")?.Trim();
        var farmAnimalName = ReadOptionalString(whenEl, "farmAnimalName", issues, $"{whenPath}.farmAnimalName")?.Trim();

        int? day = null;
        if (TryGetPropertyIgnoreCase(whenEl, "day", out var dayEl))
        {
            if (dayEl.ValueKind == JsonValueKind.Number && dayEl.TryGetInt32(out var d))
            {
                if (d is < 1 or > 28)
                {
                    issues.Add(
                        new ProduceOverrideIssue(
                            ProduceOverrideIssueSeverity.Error,
                            "invalid_day",
                            "Field 'day' must be in the range 1..28.",
                            $"{whenPath}.day"
                        )
                    );
                }
                else
                {
                    day = d;
                }
            }
            else
            {
                issues.Add(
                    new ProduceOverrideIssue(
                        ProduceOverrideIssueSeverity.Error,
                        "invalid_day",
                        "Field 'day' must be an integer in the range 1..28.",
                        $"{whenPath}.day"
                    )
                );
            }
        }

        ProduceOverrideProduceType? produceType = null;
        var produceTypeRaw = ReadOptionalString(whenEl, "produceType", issues, $"{whenPath}.produceType")?.Trim();
        if (!string.IsNullOrWhiteSpace(produceTypeRaw))
        {
            if (produceTypeRaw.Equals("produce", StringComparison.OrdinalIgnoreCase))
                produceType = ProduceOverrideProduceType.Produce;
            else if (produceTypeRaw.Equals("deluxe", StringComparison.OrdinalIgnoreCase))
                produceType = ProduceOverrideProduceType.Deluxe;
            else
            {
                issues.Add(
                    new ProduceOverrideIssue(
                        ProduceOverrideIssueSeverity.Error,
                        "invalid_produce_type",
                        "Field 'produceType' must be either 'produce' or 'deluxe'.",
                        $"{whenPath}.produceType"
                    )
                );
            }
        }

        WarnUnknownProperties(
            whenEl,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "roosterType",
                "buildingType",
                "locationName",
                "farmAnimalName",
                "day",
                "produceType"
            },
            issues,
            whenPath
        );

        ValidateNonEmptyString("roosterType", roosterType, issues, $"{whenPath}.roosterType");
        ValidateNonEmptyString("buildingType", buildingType, issues, $"{whenPath}.buildingType");
        ValidateNonEmptyString("locationName", locationName, issues, $"{whenPath}.locationName");
        ValidateNonEmptyString("farmAnimalName", farmAnimalName, issues, $"{whenPath}.farmAnimalName");

        return new ProduceOverrideWhen(
            RoosterType: roosterType,
            BuildingType: buildingType,
            LocationName: locationName,
            FarmAnimalName: farmAnimalName,
            Day: day,
            ProduceType: produceType
        );
    }

    private static ProduceOverrideThen? ParseThen(JsonElement ruleObj, List<ProduceOverrideIssue> issues, string rulePath)
    {
        if (!TryGetPropertyIgnoreCase(ruleObj, "then", out var thenEl))
        {
            issues.Add(
                new ProduceOverrideIssue(
                    ProduceOverrideIssueSeverity.Error,
                    "missing_then",
                    "Each rule must contain a 'then' object describing what to do when it matches.",
                    $"{rulePath}.then"
                )
            );
            return null;
        }

        var thenPath = $"{rulePath}.then";

        if (thenEl.ValueKind == JsonValueKind.Null)
        {
            issues.Add(
                new ProduceOverrideIssue(
                    ProduceOverrideIssueSeverity.Error,
                    "missing_then",
                    "Each rule must contain a 'then' object describing what to do when it matches.",
                    thenPath
                )
            );
            return null;
        }

        if (thenEl.ValueKind != JsonValueKind.Object)
        {
            issues.Add(
                new ProduceOverrideIssue(
                    ProduceOverrideIssueSeverity.Error,
                    "invalid_then",
                    "Rule 'then' must be an object.",
                    thenPath
                )
            );
            return null;
        }

        var excludeKeys = ReadStringArray(thenEl, "excludeKeys", issues, $"{thenPath}.excludeKeys");
        var replaceKeys = ReadStringMap(thenEl, "replaceKeys", issues, $"{thenPath}.replaceKeys");
        var multipliers = ReadDoubleMap(thenEl, "weightMultipliers", issues, $"{thenPath}.weightMultipliers");
        var hardOverrideKey = ReadOptionalString(thenEl, "hardOverrideKey", issues, $"{thenPath}.hardOverrideKey")?.Trim();

        ValidateNonEmptyString("hardOverrideKey", hardOverrideKey, issues, $"{thenPath}.hardOverrideKey");

        WarnUnknownProperties(
            thenEl,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "excludeKeys", "replaceKeys", "weightMultipliers", "hardOverrideKey" },
            issues,
            thenPath
        );

        return new ProduceOverrideThen(
            ExcludeKeys: excludeKeys,
            ReplaceKeys: replaceKeys,
            WeightMultipliers: multipliers,
            HardOverrideKey: hardOverrideKey
        );
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement obj, string propertyName, List<ProduceOverrideIssue> issues, string path)
    {
        if (!TryGetPropertyIgnoreCase(obj, propertyName, out var el))
            return Array.Empty<string>();

        if (el.ValueKind == JsonValueKind.Null)
            return Array.Empty<string>();

        if (el.ValueKind != JsonValueKind.Array)
        {
            issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "invalid_array", $"Field '{propertyName}' must be an array of strings.", path));
            return Array.Empty<string>();
        }

        var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        var idx = 0;
        foreach (var item in el.EnumerateArray())
        {
            var itemPath = $"{path}[{idx}]";
            if (item.ValueKind != JsonValueKind.String)
            {
                issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "invalid_array_item", $"Field '{propertyName}' must contain strings only.", itemPath));
                idx++;
                continue;
            }

            var s = (item.GetString() ?? string.Empty).Trim();
            if (s.Length == 0)
            {
                issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "empty_string", $"Field '{propertyName}' must not contain empty strings.", itemPath));
                idx++;
                continue;
            }

            if (!set.Add(s))
            {
                issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Warning, "duplicate_value", $"Duplicate value '{s}' ignored.", itemPath));
                idx++;
                continue;
            }

            idx++;
        }

        // SortedSet already enforces deterministic ordering.
        foreach (var s in set)
            result.Add(s);

        return result;
    }

    private static IReadOnlyDictionary<string, string> ReadStringMap(JsonElement obj, string propertyName, List<ProduceOverrideIssue> issues, string path)
    {
        if (!TryGetPropertyIgnoreCase(obj, propertyName, out var el))
            return new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (el.ValueKind == JsonValueKind.Null)
            return new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (el.ValueKind != JsonValueKind.Object)
        {
            issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "invalid_object", $"Field '{propertyName}' must be an object map.", path));
            return new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var map = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in el.EnumerateObject())
        {
            var key = prop.Name.Trim();
            var valuePath = $"{path}.{prop.Name}";

            if (key.Length == 0)
            {
                issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "empty_key", $"Field '{propertyName}' contains an empty key.", valuePath));
                continue;
            }

            if (prop.Value.ValueKind != JsonValueKind.String)
            {
                issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "invalid_map_value", $"Field '{propertyName}' values must be strings.", valuePath));
                continue;
            }

            var value = (prop.Value.GetString() ?? string.Empty).Trim();
            if (value.Length == 0)
            {
                issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "empty_string", $"Field '{propertyName}' must not contain empty string values.", valuePath));
                continue;
            }

            if (map.TryGetValue(key, out _))
            {
                issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Warning, "duplicate_key", $"Duplicate key '{key}' overwritten by later value.", valuePath));
            }

            map[key] = value;
        }

        return map;
    }

    private static IReadOnlyDictionary<string, double> ReadDoubleMap(JsonElement obj, string propertyName, List<ProduceOverrideIssue> issues, string path)
    {
        if (!TryGetPropertyIgnoreCase(obj, propertyName, out var el))
            return new SortedDictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        if (el.ValueKind == JsonValueKind.Null)
            return new SortedDictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        if (el.ValueKind != JsonValueKind.Object)
        {
            issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "invalid_object", $"Field '{propertyName}' must be an object map.", path));
            return new SortedDictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }

        var map = new SortedDictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in el.EnumerateObject())
        {
            var key = prop.Name.Trim();
            var valuePath = $"{path}.{prop.Name}";

            if (key.Length == 0)
            {
                issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "empty_key", $"Field '{propertyName}' contains an empty key.", valuePath));
                continue;
            }

            if (prop.Value.ValueKind != JsonValueKind.Number)
            {
                issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "invalid_map_value", $"Field '{propertyName}' values must be numbers.", valuePath));
                continue;
            }

            if (!prop.Value.TryGetDouble(out var value) || double.IsNaN(value) || double.IsInfinity(value))
            {
                issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "invalid_multiplier", $"Field '{propertyName}' contains an invalid multiplier for key '{key}'.", valuePath));
                continue;
            }

            if (value <= 0)
            {
                issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "invalid_multiplier", $"Field '{propertyName}' multiplier for key '{key}' must be > 0.", valuePath));
                continue;
            }

            if (map.TryGetValue(key, out _))
            {
                issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Warning, "duplicate_key", $"Duplicate key '{key}' overwritten by later value.", valuePath));
            }

            map[key] = value;
        }

        return map;
    }

    private static string? ReadOptionalString(JsonElement obj, string propertyName, List<ProduceOverrideIssue> issues, string path)
    {
        if (!TryGetPropertyIgnoreCase(obj, propertyName, out var el))
            return null;

        if (el.ValueKind == JsonValueKind.Null)
            return null;

        if (el.ValueKind != JsonValueKind.String)
        {
            issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "invalid_string", $"Field '{propertyName}' must be a string.", path));
            return null;
        }

        return el.GetString();
    }

    private static void ValidateNonEmptyString(string fieldName, string? value, List<ProduceOverrideIssue> issues, string path)
    {
        if (value is null)
            return;

        if (value.Trim().Length == 0)
        {
            issues.Add(new ProduceOverrideIssue(ProduceOverrideIssueSeverity.Error, "empty_string", $"Field '{fieldName}' must be a non-empty string when present.", path));
        }
    }

    private static void WarnUnknownProperties(JsonElement obj, HashSet<string> known, List<ProduceOverrideIssue> issues, string path)
    {
        if (obj.ValueKind != JsonValueKind.Object)
            return;

        foreach (var prop in obj.EnumerateObject())
        {
            if (!known.Contains(prop.Name))
            {
                issues.Add(
                    new ProduceOverrideIssue(
                        ProduceOverrideIssueSeverity.Warning,
                        "unknown_field",
                        $"Unknown field '{prop.Name}' ignored.",
                        $"{path}.{prop.Name}"
                    )
                );
            }
        }
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement obj, string propertyName, out JsonElement value)
    {
        if (obj.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        // JsonElement has only case-sensitive lookup; enumerate for case-insensitivity.
        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.NameEquals(propertyName) || prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}

/// <summary>
/// Validated rule set. Rules are stored in the JSON list order for deterministic application.
/// </summary>
public sealed record ProduceOverrideRuleSet(IReadOnlyList<ProduceOverrideRule> Rules);

/// <summary>One produce override rule.</summary>
public sealed record ProduceOverrideRule(int OrderIndex, string? Id, ProduceOverrideWhen When, ProduceOverrideThen Then);

/// <summary>
/// Match conditions for a rule. All provided fields are combined with AND semantics.
/// </summary>
public sealed record ProduceOverrideWhen(
    string? RoosterType = null,
    string? BuildingType = null,
    string? LocationName = null,
    string? FarmAnimalName = null,
    int? Day = null,
    ProduceOverrideProduceType? ProduceType = null
);

/// <summary>
/// Actions to apply when a rule matches.
/// </summary>
public sealed record ProduceOverrideThen(
    IReadOnlyList<string> ExcludeKeys,
    IReadOnlyDictionary<string, string> ReplaceKeys,
    IReadOnlyDictionary<string, double> WeightMultipliers,
    string? HardOverrideKey
)
{
    public bool IsEmpty
        => (ExcludeKeys.Count == 0)
            && (ReplaceKeys.Count == 0)
            && (WeightMultipliers.Count == 0)
            && string.IsNullOrWhiteSpace(HardOverrideKey);
}

/// <summary>
/// Narrow selector for which table this rule targets.
/// </summary>
public enum ProduceOverrideProduceType
{
    Produce,
    Deluxe
}

public enum ProduceOverrideIssueSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>One parse/validation issue produced by <see cref="ProduceOverrideRules.Parse"/>.</summary>
public sealed record ProduceOverrideIssue(
    ProduceOverrideIssueSeverity Severity,
    string Code,
    string Message,
    string JsonPath
);

/// <summary>Result of parsing and validating rules JSON.</summary>
public sealed record ProduceOverrideParseResult(ProduceOverrideRuleSet RuleSet, IReadOnlyList<ProduceOverrideIssue> Issues)
{
    public static ProduceOverrideParseResult ValidEmpty { get; } =
        new(new ProduceOverrideRuleSet(Array.Empty<ProduceOverrideRule>()), Array.Empty<ProduceOverrideIssue>());

    public bool IsValid
    {
        get
        {
            foreach (var issue in Issues)
            {
                if (issue.Severity == ProduceOverrideIssueSeverity.Error)
                    return false;
            }

            return true;
        }
    }
}
