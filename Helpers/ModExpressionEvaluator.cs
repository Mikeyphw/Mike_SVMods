using System.Text.RegularExpressions;
using StardewModdingAPI;

namespace Mikew.BirdsOfAFeather.Helpers
{
    public static class ModIdEvaluator
    {
        public static bool Evaluate(string expression)
        {
            // Remove whitespace from the expression
            expression = expression.Replace(" ", "");
            if (expression.ToLower() == "vanilla")
            {
                return true;
            }
            // Use a recursive descent parser to evaluate the expression
            return new Parser(expression)._Evaluate();
        }

        private sealed class Parser
        {
            private readonly string _expression;
            private int _index;

            public Parser(string expression)
            {
                _expression = expression;
            }

            public bool _Evaluate()
            {
                return EvaluateExpression();
            }

            private bool EvaluateExpression()
            {
                bool value = EvaluateTerm();

                while (
                    _index < _expression.Length
                    && (
                        _expression.Substring(_index).StartsWith("&&")
                        || _expression.Substring(_index).StartsWith("||")
                    )
                )
                {
                    string op = _expression.Substring(_index, 2);
                    _index += 2;
                    bool term = EvaluateTerm();

                    if (op == "&&")
                    {
                        value = value && term;
                    }
                    else
                    {
                        value = value || term;
                    }
                }

                return value;
            }

            private bool EvaluateTerm()
            {
                bool value;
                if (string.IsNullOrEmpty(_expression))
                {
                    ModEntry.MonitorInstance?.Log($"Expression {_expression} is  null or empty", LogLevel.Debug);
                    throw new ArgumentException("Expression cannot be null or empty.");
                }
                

                if (_expression[_index] == '(')
                {
                    _index++;
                    value = EvaluateExpression();

                    // Check for closing parenthesis
                    if (_index >= _expression.Length || _expression[_index] != ')')
                    {
                        throw new Exception("Expected ')'");
                    }

                    _index++;
                }
                else
                {
                    string? modId = GetModId();

                    if (string.IsNullOrEmpty(modId))
                    {
                        throw new Exception("Expected ModId");
                    }

                    value = ModEntry.HelperInstance!.ModRegistry.IsLoaded(modId) || modId.ToLower()=="vanilla";
                }

                return value;
            }

            private string? GetModId()
            {
                var match = Regex.Match(_expression.Substring(_index), @"^[a-zA-Z0-9_.]+");

                if (match.Success)
                {
                    _index += match.Length;
                    return match.Value;
                }

                return null;
            }
        }
    }
}
