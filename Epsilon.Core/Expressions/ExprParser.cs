namespace Epsilon.Core;

/// <summary>Parses math written as text, such as <c>"2x^2 + sin(x) - 1"</c>, into an <see cref="Expr"/>.</summary>
public static class ExprParser
{
    private static readonly string[] ReservedIdentifiers = new[]
    {
        "nthroot", "sqrt", "asinh", "acosh", "atanh", "asin", "acos", "atan",
        "sinh", "cosh", "tanh", "coth", "sech", "csch",
        "sin", "cos", "tan", "cot", "sec", "csc", "exp", "ln", "pi", "π", "e", "i",
        "sign", "floor", "ceiling", "round", "min", "max", "log", "abs"
    }.OrderByDescending(s => s.Length).ToArray();

    private static readonly HashSet<string> FunctionNames = new(new[]
    {
        "nthroot", "sqrt", "asinh", "acosh", "atanh", "asin", "acos", "atan",
        "sinh", "cosh", "tanh", "coth", "sech", "csch",
        "sin", "cos", "tan", "cot", "sec", "csc", "exp", "ln",
        "sign", "floor", "ceiling", "round", "min", "max", "log", "abs"
    });

    private static readonly HashSet<string> TwoArgumentFunctions = ["min", "max", "log", "nthroot"];

    // Parsing, Canonicalize, Evaluate, Print and ToLatex are recursive, and a
    // StackOverflowException kills the process. Both limits are far beyond hand-written math,
    // yet leave enough stack for those operations on a 1 MB thread stack (the default on
    // Windows), so untrusted input can't crash the caller. Simplify and Differentiate build
    // deeper trees than their input and aren't covered: on input near the limits they can
    // still overflow a 1 MB stack.
    private const int MaxNestingDepth = 256;
    private const int MaxTreeDepth = 500;

    /// <summary>
    /// Parses <paramref name="input"/> into a canonicalized expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports <c>+ - * / ^</c>, parentheses, <c>|x|</c> for abs, implicit multiplication
    /// (<c>2x</c>, <c>2(x+1)</c>, <c>x y</c>), decimals and scientific notation (<c>1.5</c>, <c>1e-5</c>),
    /// the constants <c>pi</c> (or <c>π</c>), <c>e</c> and <c>i</c>, and the functions sin, cos, tan, cot,
    /// sec, csc, their inverses asin, acos, atan, hyperbolic sinh ... csch and asinh, acosh, atanh,
    /// exp, ln, sqrt, abs, sign, floor, ceiling, round, and the two-argument min(a, b), max(a, b),
    /// log(x, base) and nthroot(x, n).
    /// </para>
    /// <para>
    /// Without <paramref name="variableNames"/>, every other single letter is a variable and
    /// <c>xy</c> means <c>x*y</c>. With them, only the listed names are variables and any other
    /// identifier is an error, which catches typos such as <c>sen(x)</c>.
    /// </para>
    /// </remarks>
    /// <param name="input">The text to parse, in invariant culture (decimal point, not comma).</param>
    /// <param name="variableNames">
    /// The allowed variable names; may be longer than one letter (<c>"theta"</c>). Empty means
    /// "any single letter".
    /// </param>
    /// <exception cref="FormatException">
    /// The input is malformed: unbalanced parentheses, an unknown identifier, a wrong number of
    /// function arguments, a missing operator between numbers (<c>2 3</c>), an invalid number.
    /// Also thrown when the input is nested more than 256 levels deep (parentheses, functions,
    /// signs and powers) or its tree is more than 500 levels deep (such as a chain of more than
    /// 500 terms), so that parsing untrusted input can't overflow the stack.
    /// </exception>
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

        // Long chains such as "x - x - ... - x" are built by loops, not recursion, so the
        // nesting limit doesn't see them - check the finished tree before recursing into it.
        if (ExceedsDepth(result, MaxTreeDepth))
            throw new FormatException(
                $"Expression is too deep (at most {MaxTreeDepth} levels); split it into smaller parts.");

        return result.Canonicalize();
    }

    // Iterative on purpose: a recursive walk would overflow on exactly the trees it rejects.
    private static bool ExceedsDepth(Expr root, int maxDepth)
    {
        var pending = new Stack<(Expr Node, int Depth)>();
        pending.Push((root, 1));

        while (pending.Count > 0)
        {
            var (node, depth) = pending.Pop();
            if (depth > maxDepth)
                return true;

            foreach (Expr child in node.Children)
                pending.Push((child, depth + 1));
        }

        return false;
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
                i = SkipExponent(input, i);
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

    // Scientific notation: "1e-5", "2.5E3". The exponent belongs to the number only when
    // e/E is followed by digits (optionally signed); otherwise "2e", "2ex" and "2e-x" keep
    // meaning 2*e..., so Euler's number still works with implicit multiplication.
    private static int SkipExponent(string input, int i)
    {
        if (i >= input.Length || input[i] is not ('e' or 'E'))
            return i;

        int j = i + 1;
        if (j < input.Length && input[j] is '+' or '-') j++;
        if (j >= input.Length || !char.IsDigit(input[j]))
            return i;

        while (j < input.Length && char.IsDigit(input[j])) j++;
        return j;
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

            // With declared variables, anything else is a typo ("sen(x)", an undeclared "y"),
            // not a product of single-letter variables - report it instead of guessing.
            if (sortedVariables.Length > 0)
                throw new FormatException(
                    $"Unknown identifier '{run[pos..]}' at position {startPos + pos}. " +
                    $"Declared variables: {string.Join(", ", sortedVariables)}.");

            // Without declared variables every other letter is its own variable: "xy" = x*y.
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
                    // "2 3" is almost certainly a missing operator, not 6.
                    if (IsNumericLiteral(Current!) && IsNumericLiteral(tokens[_pos - 1]))
                        throw new FormatException(
                            $"Missing operator between numbers '{tokens[_pos - 1]}' and '{Current}'.");

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

        // "12", "1.5", ".5", "5.", optionally with an exponent ("1e-5", "2.5E3").
        private static bool IsNumericLiteral(string token) =>
            NumericLiteral.IsMatch(token);

        private static readonly System.Text.RegularExpressions.Regex NumericLiteral =
            new(@"^(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?$");

        // Far beyond double's range (~1e308), yet small enough that the exact Rational
        // stays cheap - "1e999999999" would otherwise build a billion-digit BigInteger.
        private const int MaxDecimalExponent = 1000;

        private static Rational ParseNumber(string token)
        {
            int expIndex = token.IndexOfAny(['e', 'E']);
            if (expIndex >= 0)
            {
                string digits = token[(expIndex + 1)..].TrimStart('+', '-').TrimStart('0');
                if (digits.Length > 4 || (digits.Length > 0 && int.Parse(digits) > MaxDecimalExponent))
                    throw new FormatException(
                        $"Exponent of '{token}' is out of range (at most {MaxDecimalExponent} in magnitude).");
            }

            return Rational.FromDecimalString(token);
        }

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

        // Current recursion depth of ParseUnary. Every recursive path - parentheses, |x|,
        // function arguments, unary minus and exponents - passes through ParseUnary.
        // The top-level call isn't a nesting level, so "x" inside 256 parentheses still parses.
        private int _depth = 0;

        // unary := '-' unary | primary
        private Expr ParseUnary()
        {
            if (_depth > MaxNestingDepth)
                throw new FormatException(
                    $"Expression is nested too deeply (at most {MaxNestingDepth} levels of " +
                    "parentheses, functions, signs and powers).");

            _depth++;
            try
            {
                if (Current == "-")
                {
                    Consume();
                    return new Negate(ParseUnary());
                }
                return ParsePower();
            }
            finally
            {
                _depth--;
            }
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
                return new Constant(ParseNumber(token));
            }

            if (char.IsDigit(token[0]) || token[0] == '.')
                throw new FormatException($"Invalid number '{token}'.");

            // "π" is accepted too, since that's how Printer outputs pi.
            if (token is "pi" or "π")
            {
                Consume();
                return new Pi();
            }

            if (token == "e")
            {
                Consume();
                return new EulerNumber();
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

                // Otherwise sin(x, 2) would silently become sin(x).
                if (second is not null && !TwoArgumentFunctions.Contains(token))
                    throw new FormatException($"Function '{token}' takes exactly 1 argument.");

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

            // Declared variables only, when any were declared; otherwise any single letter.
            if (knownVariables.Count > 0
                    ? knownVariables.Contains(token)
                    : token.Length == 1 && char.IsLetter(token[0]))
            {
                Consume();
                return new Variable(token);
            }

            throw new FormatException($"Unexpected token '{token}'.");
        }
    }
}