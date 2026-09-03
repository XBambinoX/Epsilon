namespace Epsilon;

public static class ExprParser
{
    public static Expr Parse(string input)
    {
        var tokens = Tokenize(input);
        var parser = new Parser(tokens);
        Expr result = parser.ParseExpression();
        parser.ExpectEnd();
        return result;
    }

    private static List<string> Tokenize(string input)
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
                tokens.Add(input[start..i]);
                continue;
            }

            if ("+-*/^()".Contains(c))
            {
                tokens.Add(c.ToString());
                i++;
                continue;
            }

            throw new FormatException($"Unexpected character '{c}' at position {i}.");
        }

        return tokens;
    }

    private sealed class Parser(List<string> tokens)
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

        // term := factor (('*' | '/') factor)*
        private Expr ParseTerm()
        {
            Expr left = ParsePower();
            while (Current is "*" or "/")
            {
                string op = Consume();
                Expr right = ParsePower();
                left = op == "*" ? new Multiply(left, right) : new Divide(left, right);
            }
            return left;
        }

        // power := unary ('^' power)?   -- right-associative
        private Expr ParsePower()
        {
            Expr baseExpr = ParseUnary();
            if (Current == "^")
            {
                Consume();
                Expr exponent = ParsePower(); // recurse right for right-associativity
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
            return ParsePrimary();
        }

        // primary := NUMBER | 'x' | '(' expression ')'
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

            if (double.TryParse(token, out double number))
            {
                Consume();
                return new Constant(number);
            }

            if (token == "x")
            {
                Consume();
                return new Variable();
            }

            if (char.IsLetter(token[0]))
            {
                Consume(); // consume function name

                if (Current != "(")
                    throw new FormatException($"Expected '(' after function name '{token}'.");

                Consume(); // consume '('
                Expr argument = ParseExpression();

                if (Current != ")")
                    throw new FormatException($"Expected closing ')' after arguments of '{token}'.");
                Consume();

                return token switch
                {
                    "sin" => new Sin(argument),
                    "cos" => new Cos(argument),
                    "tan" => new Tan(argument),
                    "cot" => new Cot(argument),
                    "sec" => new Sec(argument),
                    "csc" => new Csc(argument),
                    "asin" => new Asin(argument),
                    "acos" => new Acos(argument),
                    "atan" => new Atan(argument),
                    "sinh" => new Sinh(argument),
                    "cosh" => new Cosh(argument),
                    "tanh" => new Tanh(argument),
                    _ => throw new FormatException($"Unknown function '{token}'.")
                };
            }
    
            throw new FormatException($"Unexpected token '{token}'.");
        }
    }
}