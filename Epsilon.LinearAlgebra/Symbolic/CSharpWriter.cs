using Epsilon.Core;

namespace Epsilon.LinearAlgebra;

// Writes a matrix of expressions as the source of a C# method. Every node becomes the double
// operation of its Evaluate, with the parentheses that keep the order of evaluation, as the
// core's ExprCompiler does, so the method returns exactly the values of Evaluate. A subtree that
// occurs more than once is computed once, into a local.
internal sealed class CSharpWriter
{
    private const int Additive = 1, Multiplicative = 2, Prefix = 3, Primary = 4;

    private static readonly Dictionary<Type, (string Operator, int Precedence)> Operators = new()
    {
        [typeof(Add)] = ("+", Additive),
        [typeof(Subtract)] = ("-", Additive),
        [typeof(Multiply)] = ("*", Multiplicative),
        [typeof(Divide)] = ("/", Multiplicative),
    };

    private static readonly Dictionary<Type, string> BinaryFunctions = new()
    {
        [typeof(Power)] = "Math.Pow",
        [typeof(Min)] = "Math.Min",
        [typeof(Max)] = "Math.Max",
    };

    // Reciprocal: the node is 1 / the function, as cot = 1 / tan.
    private static readonly Dictionary<Type, (string Function, bool Reciprocal)> Functions = new()
    {
        [typeof(Sqrt)] = ("Math.Sqrt", false),
        [typeof(Exp)] = ("Math.Exp", false),
        [typeof(Ln)] = ("Math.Log", false),
        [typeof(Abs)] = ("Math.Abs", false),
        [typeof(Floor)] = ("Math.Floor", false),
        [typeof(Ceiling)] = ("Math.Ceiling", false),
        [typeof(Sin)] = ("Math.Sin", false),
        [typeof(Cos)] = ("Math.Cos", false),
        [typeof(Tan)] = ("Math.Tan", false),
        [typeof(Cot)] = ("Math.Tan", true),
        [typeof(Sec)] = ("Math.Cos", true),
        [typeof(Csc)] = ("Math.Sin", true),
        [typeof(Asin)] = ("Math.Asin", false),
        [typeof(Acos)] = ("Math.Acos", false),
        [typeof(Atan)] = ("Math.Atan", false),
        [typeof(Sinh)] = ("Math.Sinh", false),
        [typeof(Cosh)] = ("Math.Cosh", false),
        [typeof(Tanh)] = ("Math.Tanh", false),
        [typeof(Coth)] = ("Math.Tanh", true),
        [typeof(Sech)] = ("Math.Cosh", true),
        [typeof(Csch)] = ("Math.Sinh", true),
        [typeof(Asinh)] = ("Math.Asinh", false),
        [typeof(Acosh)] = ("Math.Acosh", false),
        [typeof(Atanh)] = ("Math.Atanh", false),
    };

    // Written as @name when used as identifiers. Contextual keywords such as var are fine as they are.
    private static readonly HashSet<string> Keywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
        "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
        "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
        "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
        "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly",
        "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
        "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while",
    ];

    // The same as the core's NthRoot.RealRoot, which the generated code can't call.
    private const string RealRootBody =
        """
            static double {0}(double argument, double degree)
            {{
                if (argument < 0 && Math.Floor(degree) == degree && Math.Abs(degree % 2) == 1)
                    return -{0}(-argument, degree);

                double root = Math.Pow(argument, 1.0 / degree);
                double nearest = Math.Round(root);
                return Math.Pow(nearest, degree) == argument ? nearest : root;
            }}
        """;

    private readonly HashSet<string> _names;
    private readonly Dictionary<Expr, int> _occurrences = [];
    private readonly Dictionary<Expr, string> _locals = [];
    private readonly List<string> _statements = [];
    private string? _realRoot;
    private int _nextLocal;

    private CSharpWriter(HashSet<string> names)
    {
        _names = names;
    }

    internal static string Write(Matrix<Expr> matrix, string name, IReadOnlyList<string> variables)
    {
        CheckIdentifier(name, nameof(name));
        ArgumentNullException.ThrowIfNull(variables);

        string[] parameters = [.. variables];
        var names = new HashSet<string> { name };
        foreach (string variable in parameters)
        {
            CheckIdentifier(variable, nameof(variables));
            if (!names.Add(variable))
                throw new ArgumentException(
                    variable == name ? $"'{name}' is both the name of the method and a variable." : $"Variable '{variable}' is listed twice.",
                    nameof(variables));
        }

        foreach (string variable in matrix.GetVariables().Order(StringComparer.Ordinal))
            if (!parameters.Contains(variable))
                throw new ArgumentException(
                    $"An entry has the variable '{variable}', which is not in the list of variables [{string.Join(", ", parameters)}].",
                    nameof(variables));

        var writer = new CSharpWriter(names);
        string result = writer.Unique("result");
        foreach (Expr entry in matrix.Entries)
            writer.CountOccurrences(entry);

        int index = 0;
        foreach (Expr entry in matrix.Entries)
            writer._statements.Add($"{result}[{index++}] = {writer.Emit(entry).Code};");

        var source = new System.Text.StringBuilder();
        string signature = string.Join(", ", parameters.Select(p => "double " + Escape(p)).Append("Span<double> " + result));
        source.Append($"public static void {Escape(name)}({signature})\n{{\n");
        foreach (string statement in writer._statements)
            source.Append("    ").Append(statement).Append('\n');
        if (writer._realRoot is not null)
            source.Append('\n').Append(string.Format(RealRootBody, writer._realRoot).ReplaceLineEndings("\n")).Append('\n');
        source.Append('}');

        return source.ToString();
    }

    private static void CheckIdentifier(string name, string paramName)
    {
        ArgumentNullException.ThrowIfNull(name, paramName);

        if (name.Length == 0 || !(char.IsLetter(name[0]) || name[0] == '_') || !name.All(c => char.IsLetterOrDigit(c) || c == '_'))
            throw new ArgumentException(
                $"'{name}' is not a valid C# name: use letters, digits and underscores, starting with a letter or an underscore.",
                paramName);
    }

    private static string Escape(string name) => Keywords.Contains(name) ? "@" + name : name;

    // The name itself, or the name with the smallest number appended that is still free.
    private string Unique(string name)
    {
        string candidate = name;
        for (int i = 1; !_names.Add(candidate); i++)
            candidate = name + i;

        return candidate;
    }

    private void CountOccurrences(Expr expr)
    {
        // Numbers, variables and constants cost nothing to repeat.
        if (expr is Constant or Variable or Pi or EulerNumber)
            return;

        if (_occurrences.TryGetValue(expr, out int count))
        {
            // Not counted inside again: a repeated subtree is computed only once.
            _occurrences[expr] = count + 1;
            return;
        }

        _occurrences[expr] = 1;
        foreach (Expr child in expr.Children)
            CountOccurrences(child);
    }

    private (string Code, int Precedence) Emit(Expr expr)
    {
        if (_locals.TryGetValue(expr, out string? local))
            return (local, Primary);

        var (code, precedence) = EmitNode(expr);
        if (_occurrences.GetValueOrDefault(expr) < 2)
            return (code, precedence);

        // Declared right before the first statement that needs it, after the locals it uses.
        do
            local = "t" + _nextLocal++;
        while (!_names.Add(local));

        _statements.Add($"double {local} = {code};");
        _locals.Add(expr, local);
        return (local, Primary);
    }

    private (string Code, int Precedence) EmitNode(Expr expr)
    {
        Type type = expr.GetType();

        if (Functions.TryGetValue(type, out var function))
        {
            string call = $"{function.Function}({Emit(expr.Children[0]).Code})";
            return function.Reciprocal ? ("1.0 / " + call, Multiplicative) : (call, Primary);
        }

        if (Operators.TryGetValue(type, out var op))
        {
            var (left, leftPrecedence) = Emit(expr.Children[0]);
            var (right, rightPrecedence) = Emit(expr.Children[1]);

            // Parentheses wherever C# would group differently from the tree: floating-point
            // addition is not associative, so a + (b + c) keeps its parentheses.
            if (leftPrecedence < op.Precedence)
                left = $"({left})";
            if (rightPrecedence <= op.Precedence || rightPrecedence == Prefix)
                right = $"({right})";

            return ($"{left} {op.Operator} {right}", op.Precedence);
        }

        if (BinaryFunctions.TryGetValue(type, out string? binary) || type == typeof(NthRoot))
        {
            binary ??= _realRoot ??= Unique("RealRoot");
            return ($"{binary}({Emit(expr.Children[0]).Code}, {Emit(expr.Children[1]).Code})", Primary);
        }

        return expr switch
        {
            Constant constant => Literal(constant.Value.ToDouble()),
            Variable variable => (Escape(variable.Name), Primary),
            Pi => ("Math.PI", Primary),
            EulerNumber => ("Math.E", Primary),
            Negate negate => Negated(Emit(negate.Argument)),
            Round round => ($"Math.Round({Emit(round.Argument).Code}, MidpointRounding.AwayFromZero)", Primary),
            // Math.Sign returns an int, which would make a / sign(b) an integer division.
            Sign sign => ($"(double)Math.Sign({Emit(sign.Argument).Code})", Prefix),
            ImaginaryUnit => throw new InvalidOperationException("The imaginary unit has no real value, so it has no C# form."),
            _ => throw new NotSupportedException(
                $"{type.Name} has no C# form: only the operations and functions of the core can be written as code."),
        };
    }

    private static (string Code, int Precedence) Negated((string Code, int Precedence) operand) =>
        ("-" + (operand.Precedence < Primary ? $"({operand.Code})" : operand.Code), Prefix);

    private static (string Code, int Precedence) Literal(double value)
    {
        if (double.IsInfinity(value))
            return (value > 0 ? "double.PositiveInfinity" : "double.NegativeInfinity", Primary);

        // R is the shortest text that reads back as the same double.
        string text = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        if (!text.Contains('.') && !text.Contains('E'))
            text += ".0";

        return (text, text.StartsWith('-') ? Prefix : Primary);
    }
}
