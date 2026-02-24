using System;

namespace Mikew.NestCore.Helpers;

/// <summary>
/// Evaluates expressions like <c>(A && B) || C</c> where operands are mod IDs.
/// </summary>
public static partial class ModIdEvaluator
{
    public static bool Evaluate(string expression, Func<string, bool> isLoaded)
    {
        if (expression is null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        if (isLoaded is null)
        {
            throw new ArgumentNullException(nameof(isLoaded));
        }

        var span = expression.AsSpan();
        var parser = new Parser(span, isLoaded);
        var result = parser.ParseOr();

        parser.SkipWhitespace();
        if (!parser.End)
        {
            throw new FormatException($"Unexpected token at index {parser.Index} in expression: '{expression}'.");
        }

        return result;
    }

    private ref struct Parser
    {
        private readonly ReadOnlySpan<char> _s;
        private readonly Func<string, bool> _isLoaded;
        private int _i;

        public Parser(ReadOnlySpan<char> s, Func<string, bool> isLoaded)
        {
            _s = s;
            _isLoaded = isLoaded;
            _i = 0;
        }

        public int Index => _i;
        public bool End => _i >= _s.Length;

        public void SkipWhitespace()
        {
            while (_i < _s.Length && char.IsWhiteSpace(_s[_i]))
            {
                _i++;
            }
        }

        public bool ParseOr()
        {
            var left = ParseAnd();

            while (true)
            {
                SkipWhitespace();
                if (TryConsume("||"))
                {
                    var right = ParseAnd();
                    left = left || right;
                    continue;
                }

                break;
            }

            return left;
        }

        private bool ParseAnd()
        {
            var left = ParsePrimary();

            while (true)
            {
                SkipWhitespace();
                if (TryConsume("&&"))
                {
                    var right = ParsePrimary();
                    left = left && right;
                    continue;
                }

                break;
            }

            return left;
        }

        private bool ParsePrimary()
        {
            SkipWhitespace();

            if (End)
            {
                throw new FormatException("Unexpected end of expression.");
            }

            if (_s[_i] == '(')
            {
                _i++;
                var value = ParseOr();
                SkipWhitespace();

                if (End || _s[_i] != ')')
                {
                    throw new FormatException("Expected ')'.");
                }

                _i++;
                return value;
            }

            var modId = ReadIdentifier();
            if (string.IsNullOrWhiteSpace(modId))
            {
                throw new FormatException($"Expected mod ID at index {_i}.");
            }

            return string.Equals(modId, "vanilla", StringComparison.OrdinalIgnoreCase) || _isLoaded(modId);
        }

        private string ReadIdentifier()
        {
            SkipWhitespace();

            var start = _i;
            while (_i < _s.Length)
            {
                var c = _s[_i];
                if (char.IsLetterOrDigit(c) || c == '_' || c == '.')
                {
                    _i++;
                    continue;
                }

                break;
            }

            return start == _i ? string.Empty : _s[start.._i].ToString();
        }

        private bool TryConsume(string token)
        {
            if (_i + token.Length > _s.Length)
            {
                return false;
            }

            for (var j = 0; j < token.Length; j++)
            {
                if (_s[_i + j] != token[j])
                {
                    return false;
                }
            }

            _i += token.Length;
            return true;
        }
    }
}
