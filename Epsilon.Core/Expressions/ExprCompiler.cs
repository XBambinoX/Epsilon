namespace Epsilon.Core;

// Turns expression trees into delegates through System.Linq.Expressions (JIT-compiled, or
// interpreted where the runtime can't generate code, such as NativeAOT). Every node is
// computed with the same double operations in the same order as its Evaluate, so a compiled
// delegate returns exactly the values of Evaluate, only without the bindings dictionary, the
// virtual calls and the walk over the tree. A subtree that occurs more than once is computed
// once (common subexpression elimination); a node from outside the core falls back to its
// own Evaluate.
internal sealed class ExprCompiler
{
    private readonly IReadOnlyList<string> _names;
    private readonly Dictionary<string, LinqExpression> _values = [];
    // How often each subtree occurs; the ones that occur more than once are kept in a local.
    private readonly Dictionary<Expr, int> _occurrences = [];
    private readonly Dictionary<Expr, System.Linq.Expressions.ParameterExpression> _computed = [];
    private readonly List<System.Linq.Expressions.ParameterExpression> _locals = [];

    private ExprCompiler(
        IReadOnlyList<string> names, IReadOnlyList<System.Linq.Expressions.ParameterExpression> values,
        IEnumerable<Expr> expressions)
    {
        _names = names;
        for (int i = 0; i < names.Count; i++)
            _values.Add(names[i], values[i]);

        foreach (Expr expr in expressions)
            CountOccurrences(expr);
    }

    // A delegate with one double parameter per name, in order: Func<double, double>, ...
    internal static TDelegate Compile<TDelegate>(Expr expr, IReadOnlyList<string> names)
        where TDelegate : Delegate
    {
        CheckNames(names, paramName: null);

        System.Linq.Expressions.ParameterExpression[] parameters =
            [.. names.Select(name => LinqExpression.Parameter(typeof(double), name))];
        var compiler = new ExprCompiler(names, parameters, [expr]);
        LinqExpression body = compiler.Emit(expr);

        return LinqExpression.Lambda<TDelegate>(LinqExpression.Block(compiler._locals, body), parameters).Compile();
    }

    // The values come in an array, in the order of the names.
    internal static Func<double[], double> CompileForArray(Expr expr, IReadOnlyList<string> names)
    {
        CheckNames(names, "variables");

        var values = LinqExpression.Parameter(typeof(double[]), "values");
        System.Linq.Expressions.ParameterExpression[] inputs = Inputs(names);
        var compiler = new ExprCompiler(names, inputs, [expr]);

        List<LinqExpression> statements = [CheckLength(CheckValues, values, names.Count), .. Read(values, inputs)];
        statements.Add(compiler.Emit(expr));

        var body = LinqExpression.Block([.. inputs, .. compiler._locals], statements);
        return LinqExpression.Lambda<Func<double[], double>>(body, values).Compile();
    }

    // Writes the value of expressions[k] to results[k]. All values are read before the first
    // write, so results may be the values array itself.
    internal static Action<double[], double[]> CompileMany(IReadOnlyList<Expr> expressions, IReadOnlyList<string> names)
    {
        ArgumentNullException.ThrowIfNull(expressions);
        if (expressions.Any(expr => expr is null))
            throw new ArgumentNullException(nameof(expressions), "An expression is null.");
        CheckNames(names, "variables");

        var values = LinqExpression.Parameter(typeof(double[]), "values");
        var results = LinqExpression.Parameter(typeof(double[]), "results");
        System.Linq.Expressions.ParameterExpression[] inputs = Inputs(names);
        var compiler = new ExprCompiler(names, inputs, expressions);

        List<LinqExpression> statements =
        [
            CheckLength(CheckValues, values, names.Count),
            CheckLength(CheckResults, results, expressions.Count),
            .. Read(values, inputs),
        ];
        for (int k = 0; k < expressions.Count; k++)
            statements.Add(LinqExpression.Assign(
                LinqExpression.ArrayAccess(results, LinqExpression.Constant(k)), compiler.Emit(expressions[k])));

        var body = LinqExpression.Block([.. inputs, .. compiler._locals], statements);
        return LinqExpression.Lambda<Action<double[], double[]>>(body, values, results).Compile();
    }

    private static void CheckNames(IReadOnlyList<string> names, string? paramName)
    {
        ArgumentNullException.ThrowIfNull(names, paramName);

        var seen = new HashSet<string>();
        foreach (string name in names)
        {
            if (name is null)
                throw new ArgumentNullException(paramName, "A variable name is null.");
            if (!seen.Add(name))
                throw new ArgumentException($"Variable '{name}' is listed twice.", paramName);
        }
    }

    private static System.Linq.Expressions.ParameterExpression[] Inputs(IReadOnlyList<string> names) =>
        [.. names.Select(name => LinqExpression.Variable(typeof(double), name))];

    private static IEnumerable<LinqExpression> Read(
        System.Linq.Expressions.ParameterExpression values, System.Linq.Expressions.ParameterExpression[] inputs) =>
        inputs.Select((input, i) => (LinqExpression)LinqExpression.Assign(
            input, LinqExpression.ArrayIndex(values, LinqExpression.Constant(i))));

    private static LinqExpression CheckLength(Action<double[], int> check, LinqExpression array, int length) =>
        LinqExpression.Call(check.Method, array, LinqExpression.Constant(length));

    private static void CheckValues(double[] values, int count)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length != count)
            throw new ArgumentException($"Expected {count} values, one per variable, got {values.Length}.", nameof(values));
    }

    private static void CheckResults(double[] results, int count)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (results.Length != count)
            throw new ArgumentException($"Expected an array of {count} results, one per expression, got {results.Length}.", nameof(results));
    }

    private void CountOccurrences(Expr expr)
    {
        // Numbers, variables and pi cost nothing to repeat.
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

    private LinqExpression Emit(Expr expr)
    {
        if (_computed.TryGetValue(expr, out var local))
            return local;

        LinqExpression value = EmitNode(expr);
        if (_occurrences.GetValueOrDefault(expr) < 2)
            return value;

        // The first occurrence computes and keeps the value. Emitting goes left to right,
        // depth first, which is the order the compiled code runs in, so every later
        // occurrence runs after this one and reads the local.
        local = LinqExpression.Variable(typeof(double));
        _locals.Add(local);
        _computed.Add(expr, local);
        return LinqExpression.Assign(local, value);
    }

    // The tables below must mirror the Evaluate of every node exactly; CompileTests compares
    // them bit for bit. Tables rather than one big switch keep the stack frame of this
    // recursion small, so expressions as deep as the parser allows compile on a 1 MB stack.
    private static readonly Dictionary<Type, System.Linq.Expressions.ExpressionType> Operators = new()
    {
        [typeof(Add)] = System.Linq.Expressions.ExpressionType.Add,
        [typeof(Subtract)] = System.Linq.Expressions.ExpressionType.Subtract,
        [typeof(Multiply)] = System.Linq.Expressions.ExpressionType.Multiply,
        [typeof(Divide)] = System.Linq.Expressions.ExpressionType.Divide,
    };

    private static readonly Dictionary<Type, Func<double, double, double>> BinaryFunctions = new()
    {
        [typeof(Power)] = Math.Pow,
        [typeof(NthRoot)] = NthRoot.RealRoot,
        [typeof(Min)] = Math.Min,
        [typeof(Max)] = Math.Max,
    };

    // Reciprocal: the node is 1 / the function, as cot = 1 / tan.
    private static readonly Dictionary<Type, (Func<double, double> Function, bool Reciprocal)> Functions = new()
    {
        [typeof(Sqrt)] = (Math.Sqrt, false),
        [typeof(Exp)] = (Math.Exp, false),
        [typeof(Ln)] = (Math.Log, false),
        [typeof(Abs)] = (Math.Abs, false),
        [typeof(Floor)] = (Math.Floor, false),
        [typeof(Ceiling)] = (Math.Ceiling, false),
        [typeof(Round)] = (RoundHalfAwayFromZero, false),
        [typeof(Sign)] = (SignOf, false),
        [typeof(Sin)] = (Math.Sin, false),
        [typeof(Cos)] = (Math.Cos, false),
        [typeof(Tan)] = (Math.Tan, false),
        [typeof(Cot)] = (Math.Tan, true),
        [typeof(Sec)] = (Math.Cos, true),
        [typeof(Csc)] = (Math.Sin, true),
        [typeof(Asin)] = (Math.Asin, false),
        [typeof(Acos)] = (Math.Acos, false),
        [typeof(Atan)] = (Math.Atan, false),
        [typeof(Sinh)] = (Math.Sinh, false),
        [typeof(Cosh)] = (Math.Cosh, false),
        [typeof(Tanh)] = (Math.Tanh, false),
        [typeof(Coth)] = (Math.Tanh, true),
        [typeof(Sech)] = (Math.Cosh, true),
        [typeof(Csch)] = (Math.Sinh, true),
        [typeof(Asinh)] = (Math.Asinh, false),
        [typeof(Acosh)] = (Math.Acosh, false),
        [typeof(Atanh)] = (Math.Atanh, false),
    };

    private LinqExpression EmitNode(Expr expr)
    {
        Type type = expr.GetType();

        if (Functions.TryGetValue(type, out var function))
        {
            LinqExpression call = LinqExpression.Call(function.Function.Method, Emit(expr.Children[0]));
            return function.Reciprocal ? LinqExpression.Divide(LinqExpression.Constant(1.0), call) : call;
        }

        // Left before right, the order Evaluate goes in.
        if (Operators.TryGetValue(type, out var op))
            return LinqExpression.MakeBinary(op, Emit(expr.Children[0]), Emit(expr.Children[1]));

        if (BinaryFunctions.TryGetValue(type, out var binary))
            return LinqExpression.Call(binary.Method, Emit(expr.Children[0]), Emit(expr.Children[1]));

        return expr switch
        {
            Constant constant => LinqExpression.Constant(constant.Value.ToDouble()),
            Variable variable => _values.TryGetValue(variable.Name, out var value) ? value : throw NotInList(variable.Name),
            Pi => LinqExpression.Constant(Math.PI),
            EulerNumber => LinqExpression.Constant(Math.E),
            ImaginaryUnit => throw new InvalidOperationException(ImaginaryUnit.NoRealValue),
            Negate => LinqExpression.Negate(Emit(expr.Children[0])),
            _ => EmitOwnEvaluate(expr),
        };
    }

    private static double RoundHalfAwayFromZero(double value) => Math.Round(value, MidpointRounding.AwayFromZero);

    // Math.Sign throws ArithmeticException for NaN, and so does Sign.Evaluate.
    private static double SignOf(double value) => Math.Sign(value);

    // A node from outside the core is evaluated by its own Evaluate, given its variables.
    private LinqExpression EmitOwnEvaluate(Expr expr)
    {
        string[] names = [.. expr.GetVariables()];
        var values = names.Select(name => _values.TryGetValue(name, out var value) ? value : throw NotInList(name));

        return LinqExpression.Call(
            new Func<Expr, string[], double[], double>(EvaluateWithBindings).Method,
            LinqExpression.Constant(expr, typeof(Expr)),
            LinqExpression.Constant(names),
            LinqExpression.NewArrayInit(typeof(double), values));
    }

    private static double EvaluateWithBindings(Expr expr, string[] names, double[] values)
    {
        var bindings = new Dictionary<string, double>(names.Length);
        for (int i = 0; i < names.Length; i++)
            bindings.Add(names[i], values[i]);

        return expr.Evaluate(bindings);
    }

    private ArgumentException NotInList(string name) =>
        new($"The expression has the variable '{name}', which is not in the list of variables [{string.Join(", ", _names)}].");
}
