using System;
using StardewModdingAPI;

namespace RoosterControl.Helpers;

/// <summary>Evaluates mod ID expressions with boolean operators.</summary>
internal static class ModIdExpression
{
    private const string AndOperator = "&&";
    private const string OrOperator = "||";

    /// <summary>Evaluates a mod ID expression.</summary>
    /// <param name="expression">The mod ID expression (e.g., "ModA", "ModA&&ModB", "ModA||ModB").</param>
    /// <param name="registry">The mod registry to check against.</param>
    /// <returns>True if the expression is satisfied, false otherwise.</returns>
    internal static bool Evaluate(string expression, IModRegistry registry)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        // Handle AND operator: all parts must be loaded
        if (expression.Contains(AndOperator, StringComparison.Ordinal))
        {
            foreach (var part in expression.Split(AndOperator, StringSplitOptions.TrimEntries))
            {
                if (!registry.IsLoaded(part))
                    return false;
            }
            return true;
        }

        // Handle OR operator: any part must be loaded
        if (expression.Contains(OrOperator, StringComparison.Ordinal))
        {
            foreach (var part in expression.Split(OrOperator, StringSplitOptions.TrimEntries))
            {
                if (registry.IsLoaded(part))
                    return true;
            }
            return false;
        }

        // Direct mod ID check
        return registry.IsLoaded(expression);
    }
}

