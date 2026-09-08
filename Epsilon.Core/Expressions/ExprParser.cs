using System.Globalization;

namespace Epsilon.Core;

public static class ExprParser
{
    private static readonly string[] ReservedIdentifiers = new[]
    {
        "nthroot", "sqrt", "abs", "asin", "acos", "atan", "sinh", "cosh", "tanh",
        "sin", "cos", "tan", "cot", "sec", "csc", "exp", "ln", "pi", "e", "i"
    }.OrderByDescending(s => s.Length).ToArray();

    private static readonly HashSet<string> FunctionNames = new(new[]
    {
        "nthroot", "sqrt", "abs", "asin", "acos", "atan", "sinh", "cosh", "tanh",
        "sin", "cos", "tan", "cot", "sec", "csc", "exp", "ln"
    });

    public static Expr Parse(string input, params string[] variableNames)
    {
        var knownVariables = variableNames.ToHashSet();
        var tokens = Tokenize(input, knownVariables);
        var parser = new Parser(tokens, knownVariables);
        Expr result = parser.ParseExpression();
        parser.ExpectEnd();
        return result.Canonicalize();
    }

    private static List<string> Tokenize(string input, IReadOnlySet<string> knownVariables)
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

                foreach (var token in SplitIdentifierRun(run, start, knownVariables))
                    tokens.Add(token);

                continue;
            }

            if ("+-*/^(),".Contains(c))
            {
                tokens.Add(c.ToString());
                i++;
                continue;
            }

            throw new FormatException($"Unexpected character '{c}' at position {i}.");
        }

        return tokens;
    }

    private static IEnumerable<string> SplitIdentifierRun(string run, int startPos, IReadOnlySet<string> knownVariables)
    {
        int pos = 0;
        var result = new List<string>();

        while (pos < run.Length)
        {
            string? reservedMatch = ReservedIdentifiers.FirstOrDefault(id =>
                pos + id.Length <= run.Length &&
                string.CompareOrdinal(run, pos, id, 0, id.Length) == 0);

            string? variableMatch = knownVariables.Count > 0
                ? knownVariables
                    .Where(v => pos + v.Length <= run.Length &&
                                string.CompareOrdinal(run, pos, v, 0, v.Length) == 0)
                    .OrderByDescending(v => v.Length)
                    .FirstOrDefault()
                : null;

            // Longest match wins, regardless of pool — a declared variable like "second"
            // must not be shadowed by the shorter reserved prefix "sec".
            string? match = (reservedMatch?.Length ?? 0) >= (variableMatch?.Length ?? 0)
                ? reservedMatch
                : variableMatch;

            if (match is not null)
            {
                result.Add(match);
                pos += match.Length;
                continue;
            }

            // Fallback: single-letter implicit-multiplication behavior, unchanged.
            result.Add(run[pos].ToString());
            pos += 1;
        }

        return result;
    }

    private sealed class Parser(List<string> tokens, IReadOnlySet<string> knownVariables)
    {
        private int _pos = 0;

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

        private static bool StartsImplicitFactor(string? token) =>
            token is not null && (token == "(" || char.IsDigit(token[0]) || char.IsLetter(token[0]));
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
                return new Subtract(new Constant(0), ParseUnary());
            }
            return ParsePower();
        }

        // primary := NUMBER | VARIABLE | 'pi' | 'e' | 'i' | FUNCTION '(' args ')' | '(' expression ')'
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

            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                Consume();
                return new Constant(number);
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

                var arguments = new List<Expr> { ParseExpression() };
                while (Current == ",")
                {
                    Consume();
                    arguments.Add(ParseExpression());
                }

                if (Current != ")")
                    throw new FormatException($"Expected closing ')' after arguments of '{token}'.");
                Consume();

                return token switch
                {
                    "sin" => new Sin(arguments[0]),
                    "cos" => new Cos(arguments[0]),
                    "tan" => new Tan(arguments[0]),
                    "cot" => new Cot(arguments[0]),
                    "sec" => new Sec(arguments[0]),
                    "csc" => new Csc(arguments[0]),
                    "asin" => new Asin(arguments[0]),
                    "acos" => new Acos(arguments[0]),
                    "atan" => new Atan(arguments[0]),
                    "sinh" => new Sinh(arguments[0]),
                    "cosh" => new Cosh(arguments[0]),
                    "tanh" => new Tanh(arguments[0]),
                    "exp" => new Exp(arguments[0]),
                    "ln" => new Ln(arguments[0]),
                    "sqrt" => new Sqrt(arguments[0]),
                    "abs" => new Abs(arguments[0]),
                    "nthroot" when arguments.Count == 2 => new NthRoot(arguments[0], arguments[1]),
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