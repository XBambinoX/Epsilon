namespace Epsilon.Core;

public static class ExprParser
{
    private static readonly string[] ReservedIdentifiers = new[]
    {
        "nthroot", "sqrt", "asinh", "acosh", "atanh", "asin", "acos", "atan",
        "sinh", "cosh", "tanh", "coth", "sech", "csch",
        "sin", "cos", "tan", "cot", "sec", "csc", "exp", "ln", "pi", "e", "i", "x",
        "sign", "floor", "ceiling", "round", "min", "max", "log", "abs"
    }.OrderByDescending(s => s.Length).ToArray();

    private static readonly HashSet<string> FunctionNames = new(new[]
    {
        "nthroot", "sqrt", "asinh", "acosh", "atanh", "asin", "acos", "atan",
        "sinh", "cosh", "tanh", "coth", "sech", "csch",
        "sin", "cos", "tan", "cot", "sec", "csc", "exp", "ln",
        "sign", "floor", "ceiling", "round", "min", "max", "log", "abs"
    });

    public static Expr Parse(string input, params string[] variableNames)
    {
        var knownVariables = variableNames.ToHashSet();

        // Sorted once per Parse call
        string[] sortedVariables = variableNames
            .OrderByDescending(v => v.Length)
            .ToArray();

        var tokens = Tokenize(input, sortedVariables);
        var parser = new Parser(tokens, knownVariables);
        Expr result = parser.ParseExpression();
        parser.ExpectEnd();
        return result.Canonicalize();
    }

    private static List<string> Tokenize(string input, string[] sortedVariables)
    {
        var tokens = new List<string>();
        int i = 0;

        while (i < input.Length)
        {
            char c = input[i];

            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (char.IsDigit(c) || c == '.')
            {
                int start = i;
                while (i < input.Length && (char.IsDigit(input[i]) || input[i] == '.')) i++;
                tokens.Add(input[start..i]);
                continue;
            }

            if (char.IsLetter(c))
            {
                int start = i;
                while (i < input.Length && char.IsLetter(input[i])) i++;
                string run = input[start..i];

                SplitIdentifierRun(run, start, sortedVariables, tokens);
                continue;
            }

            // '|' is included alongside the other single-char operator/grouping
            // tokens so it can act as the open/close delimiter for |x| (abs sugar).
            if ("+-*/^(),|".Contains(c))
            {
                tokens.Add(c.ToString());
                i++;
                continue;
            }

            throw new FormatException($"Unexpected character '{c}' at position {i}.");
        }

        return tokens;
    }

    // Appends directly to `tokens` instead of building and returning an
    // intermediate IEnumerable<string> - one fewer allocation and no
    // per-run List<string> that the caller just concatenates anyway.
    private static void SplitIdentifierRun(string run, int startPos, string[] sortedVariables, List<string> tokens)
    {
        int pos = 0;

        while (pos < run.Length)
        {
            int reservedLen = LongestMatchLength(run, pos, ReservedIdentifiers);
            int variableLen = sortedVariables.Length > 0
                ? LongestMatchLength(run, pos, sortedVariables)
                : 0;

            // Longest match wins, regardless of pool - a declared variable like "second"
            // must not be shadowed by the shorter reserved prefix "sec".
            int matchLen = Math.Max(reservedLen, variableLen);

            if (matchLen > 0)
            {
                tokens.Add(run.Substring(pos, matchLen));
                pos += matchLen;
                continue;
            }

            // Fallback: single-letter implicit-multiplication behavior, unchanged.
            tokens.Add(run[pos].ToString());
            pos += 1;
        }
    }

    // Plain loop instead of `candidates.Where(...).OrderByDescending(...).FirstOrDefault()`.
    // `candidates` is already sorted longest-first by the caller, so the first
    // structural match found is the longest one — no re-sorting per call needed.
    private static int LongestMatchLength(string run, int pos, string[] candidatesSortedByLengthDesc)
    {
        foreach (string candidate in candidatesSortedByLengthDesc)
        {
            if (pos + candidate.Length <= run.Length &&
                string.CompareOrdinal(run, pos, candidate, 0, candidate.Length) == 0)
            {
                return candidate.Length;
            }
        }

        return 0;
    }

    private sealed class Parser(List<string> tokens, IReadOnlySet<string> knownVariables)
    {
        private int _pos = 0;

        // True while parsing the contents of a |...| group. While inside one,
        // an encountered '|' can only be the closing delimiter of *this* group,
        // never the start of a new implicit-multiplication factor — otherwise
        // the term loop misreads the closing bar as opening another group and
        // runs off the end of the input (see ParsePrimary's '|' branch).
        private bool _inBar = false;

        private string? Current => _pos < tokens.Count ? tokens[_pos] : null;

        private string Consume()
        {
            if (Current is null) throw new FormatException("Unexpected end of expression.");
            return tokens[_pos++];
        }

        public void ExpectEnd()
        {
            if (Current is not null) throw new FormatException($"Unexpected token '{Current}'.");
        }

        // expression := term (('+' | '-') term)*
        public Expr ParseExpression()
        {
            Expr left = ParseTerm();
            while (Current is "+" or "-")
            {
                string op = Consume();
                Expr right = ParseTerm();
                left = op == "+" ? new Add(left, right) : new Subtract(left, right);
            }
            return left;
        }

        // term := unary (('*' | '/') unary | unary)*
        private Expr ParseTerm()
        {
            Expr left = ParseUnary();

            while (true)
            {
                if (Current is "*" or "/")
                {
                    string op = Consume();
                    Expr right = ParseUnary();
                    left = op == "*" ? new Multiply(left, right) : new Divide(left, right);
                }
                else if (StartsImplicitFactor(Current))
                {
                    Expr right = ParseUnary();
                    left = new Multiply(left, right);
                }
                else
                {
                    break;
                }
            }

            return left;
        }

        private bool StartsImplicitFactor(string? token) =>
            token is not null && (token == "(" || (token == "|" && !_inBar) || char.IsDigit(token[0]) || char.IsLetter(token[0]));

        private static bool IsNumericLiteral(string token) =>
            token.Length > 0 &&
            token.All(c => char.IsDigit(c) || c == '.') &&
            token.Count(c => c == '.') <= 1 &&
            token.Any(char.IsDigit);

        // power := primary ('^' unary)?
        private Expr ParsePower()
        {
            Expr baseExpr = ParsePrimary();
            if (Current == "^")
            {
                Consume();
                Expr exponent = ParseUnary();
                return new Power(baseExpr, exponent);
            }
            return baseExpr;
        }

        // unary := '-' unary | primary
        private Expr ParseUnary()
        {
            if (Current == "-")
            {
                Consume();
                return new Negate(ParseUnary());
            }
            return ParsePower();
        }

        // primary := NUMBER | VARIABLE | 'pi' | 'e' | 'i' | FUNCTION '(' args ')'
        //          | '(' expression ')' | '|' expression '|'
        private Expr ParsePrimary()
        {
            string? token = Current;

            if (token is null)
                throw new FormatException("Unexpected end of expression.");

            if (token == "(")
            {
                Consume();
                Expr inner = ParseExpression();
                if (Current != ")") throw new FormatException("Expected closing ')'.");
                Consume();
                return inner;
            }

            // |x| sugar over abs(x). Not re-entrant for nested bars like "|x + |y||" —
            // the first closing '|' encountered always ends the current group.
            if (token == "|")
            {
                Consume();
                bool wasInBar = _inBar;
                _inBar = true;
                Expr inner;
                try
                {
                    inner = ParseExpression();
                }
                finally
                {
                    _inBar = wasInBar;
                }
                if (Current != "|") throw new FormatException("Expected closing '|' for absolute value.");
                Consume();
                return new Abs(inner);
            }

            if (IsNumericLiteral(token))
            {
                Consume();
                return new Constant(Rational.FromDecimalString(token));
            }

            if (token == "pi")
            {
                Consume();
                return new Pi();
            }

            if (token == "e")
            {
                Consume();
                return new E();
            }

            if (token == "i")
            {
                Consume();
                return new ImaginaryUnit();
            }

            if (FunctionNames.Contains(token))
            {
                Consume();

                if (Current != "(")
                    throw new FormatException($"Expected '(' after function name '{token}'.");

                Consume();

                Expr first = ParseExpression();
                Expr? second = null;

                if (Current == ",")
                {
                    Consume();
                    second = ParseExpression();

                    if (Current == ",")
                        throw new FormatException($"Function '{token}' takes at most 2 arguments.");
                }

                if (Current != ")")
                    throw new FormatException($"Expected closing ')' after arguments of '{token}'.");
                Consume();

                return token switch
                {
                    "sin" => new Sin(first),
                    "cos" => new Cos(first),
                    "tan" => new Tan(first),
                    "cot" => new Cot(first),
                    "sec" => new Sec(first),
                    "csc" => new Csc(first),
                    "asin" => new Asin(first),
                    "acos" => new Acos(first),
                    "atan" => new Atan(first),
                    "sinh" => new Sinh(first),
                    "cosh" => new Cosh(first),
                    "tanh" => new Tanh(first),
                    "asinh" => new Asinh(first),
                    "acosh" => new Acosh(first),
                    "atanh" => new Atanh(first),
                    "coth" => new Coth(first),
                    "sech" => new Sech(first),
                    "csch" => new Csch(first),
                    "exp" => new Exp(first),
                    "ln" => new Ln(first),
                    "sqrt" => new Sqrt(first),
                    "sign" => new Sign(first),
                    "floor" => new Floor(first),
                    "ceiling" => new Ceiling(first),
                    "round" => new Round(first),
                    "abs" => new Abs(first),

                    "min" when second is not null => new Min(first, second),
                    "min" => throw new FormatException("min requires exactly 2 arguments: min(a, b)."),

                    "max" when second is not null => new Max(first, second),
                    "max" => throw new FormatException("max requires exactly 2 arguments: max(a, b)."),

                    // log(x, n) = ln(x) / ln(n) - sugar over existing nodes
                    "log" when second is not null => new Divide(new Ln(first), new Ln(second)),
                    "log" => throw new FormatException("log requires exactly 2 arguments: log(x, base)."),

                    "nthroot" when second is not null => new NthRoot(first, second),
                    "nthroot" => throw new FormatException("nthroot requires exactly 2 arguments: nthroot(x, n)."),

                    _ => throw new FormatException($"Unknown function '{token}'.")
                };
            }

            if (token.Length == 1 && char.IsLetter(token[0]))
            {
                Consume();
                return new Variable(token);
            }

            if (knownVariables.Contains(token) || (token.Length == 1 && char.IsLetter(token[0])))
            {
                Consume();
                return new Variable(token);
            }

            throw new FormatException($"Unexpected token '{token}'.");
        }
    }
}