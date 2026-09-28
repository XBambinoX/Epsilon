namespace Epsilon.Core;

public abstract class Expr : IEquatable<Expr>
{
    public abstract double Evaluate(IReadOnlyDictionary<string, double> bindings);

    public double Evaluate(double x) => Evaluate(SingleBinding(GetSingleVariable(), x));

    public double Evaluate(params (string Name, double Value)[] bindings)
    {
        var dict = new Dictionary<string, double>(bindings.Length);
        foreach (var (name, value) in bindings)
            dict[name] = value;

        return Evaluate(dict);
    }

    // The derivative of anything that doesn't depend on `variable` is exactly 0.
    // Checked here once instead of in every node, so rules like the quotient rule
    // never produce leftovers such as 0/y (which Simplify won't fold unless y is
    // provably nonzero).
    public Expr Differentiate(string variable) =>
        DependsOn(variable) ? DifferentiateCore(variable) : new Constant(0);

    protected abstract Expr DifferentiateCore(string variable);

    public Expr Differentiate()
    {
        var vars = GetVariables();
        if (vars.Count == 0)
            return new Constant(0);

        if (vars.Count == 1)
            return Differentiate(vars.First());

        throw new InvalidOperationException(
            $"Expected exactly 1 variable, found {vars.Count}: [{string.Join(", ", vars)}]. " +
            "Use the explicit-variable overload for multivariable expressions.");
    }

    public virtual Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) =>
        throw new NotImplementedException($"{GetType().Name} does not yet support complex evaluation.");

    public Complex EvaluateComplex(Complex x) => EvaluateComplex(SingleBinding(GetSingleVariable(), x));

    public Complex EvaluateComplex(params (string Name, Complex Value)[] bindings)
    {
        var dict = new Dictionary<string, Complex>(bindings.Length);
        foreach (var (name, value) in bindings)
            dict[name] = value;

        return EvaluateComplex(dict);
    }

    // ---- Tree structure ----
    //
    // Every node exposes its direct sub-expressions through Children and can rebuild itself
    // with different ones through WithChildren. Everything that only needs to walk the tree
    // (equality, hashing, ordering, substitution, variable collection, Simplify's traversal)
    // is written once against these two members, so a new node type doesn't have to be
    // registered in any of those places.

    /// <summary>
    /// Direct sub-expressions in a fixed order (empty for leaves). Immutable, like every
    /// Expr: subtrees are shared between expressions (Substitute and Simplify reuse the
    /// parts they don't change), which is only safe because nothing can modify them.
    /// </summary>
    public abstract ImmutableArray<Expr> Children { get; }

    /// <summary>
    /// A node of the same type and payload with the given children, in the order of
    /// <see cref="Children"/>. Returns this node itself if every child is reference-equal.
    /// </summary>
    public abstract Expr WithChildren(IReadOnlyList<Expr> children);

    /// <summary>
    /// Applies <paramref name="map"/> to every child and rebuilds the node only if some child
    /// actually changed (by reference) - otherwise returns this node, allocating nothing.
    /// </summary>
    public Expr MapChildren(Func<Expr, Expr> map)
    {
        ImmutableArray<Expr> children = Children;
        Expr[]? changed = null;

        for (int i = 0; i < children.Length; i++)
        {
            Expr mapped = map(children[i]);
            if (!ReferenceEquals(mapped, children[i]))
            {
                changed ??= [.. children];
                changed[i] = mapped;
            }
        }

        return changed is null ? this : WithChildren(changed);
    }

    // Node data that isn't a child, e.g. Constant.Value or Variable.Name. Nodes without
    // such data (Add, Sin, Pi, ...) don't need to override these.
    protected virtual bool PayloadEquals(Expr other) => true;
    protected virtual int PayloadHashCode() => 0;
    protected internal virtual int ComparePayload(Expr other) => 0;

    protected static readonly ImmutableArray<Expr> NoChildren = [];
    private static readonly IReadOnlySet<string> NoVariables = new HashSet<string>();

    // Shared WithChildren implementations for leaves and two-child nodes.
    protected Expr WithNoChildren(IReadOnlyList<Expr> children) =>
        children.Count == 0 ? this : throw ChildCountMismatch(0, children.Count);

    protected Expr WithTwoChildren(IReadOnlyList<Expr> children, Func<Expr, Expr, Expr> create)
    {
        if (children.Count != 2)
            throw ChildCountMismatch(2, children.Count);

        ImmutableArray<Expr> current = Children;
        return ReferenceEquals(children[0], current[0]) && ReferenceEquals(children[1], current[1])
            ? this
            : create(children[0], children[1]);
    }

    protected ArgumentException ChildCountMismatch(int expected, int actual) =>
        new($"{GetType().Name} has {expected} children, got {actual}.", "children");

    public virtual IReadOnlySet<string> GetVariables()
    {
        ImmutableArray<Expr> children = Children;
        if (children.Length == 0)
            return NoVariables;
        if (children.Length == 1)
            return children[0].GetVariables();

        var variables = new HashSet<string>();
        foreach (Expr child in children)
            variables.UnionWith(child.GetVariables());
        return variables;
    }

    public virtual bool DependsOn(string variable) => GetVariables().Contains(variable);

    public virtual Expr Substitute(string variable, Expr replacement) =>
        MapChildren(child => child.Substitute(variable, replacement));

    public string GetSingleVariable()
    {
        var vars = GetVariables();

        if (vars.Count != 1)
            throw new InvalidOperationException(
                $"Expected exactly 1 variable, found {vars.Count}: [{string.Join(", ", vars)}]. " +
                "Use the explicit-variable overload for multivariable expressions.");

        return vars.First();
    }

    private static IReadOnlyDictionary<string, T> SingleBinding<T>(string variable, T value) =>
        new Dictionary<string, T> { [variable] = value };

    // Operators
    //
    // Build expressions with ordinary C# syntax: x * x + 2 * x - 0.5. They only construct
    // nodes (x + 0 stays Add(x, 0)); call Simplify() to clean the result up.
    //
    // There is deliberately no ^ operator: in C# it is XOR and binds looser than +, so
    // x ^ 2 + 1 would silently mean x ^ (2 + 1). Use x.Pow(2) instead.

    public static Expr operator +(Expr left, Expr right) => new Add(left, right);
    public static Expr operator -(Expr left, Expr right) => new Subtract(left, right);
    public static Expr operator *(Expr left, Expr right) => new Multiply(left, right);
    public static Expr operator /(Expr left, Expr right) => new Divide(left, right);
    public static Expr operator -(Expr argument) => new Negate(argument);

    /// <summary>This expression raised to <paramref name="exponent"/>: x.Pow(2) is x^2.</summary>
    public Expr Pow(Expr exponent) => new Power(this, exponent);

    public static implicit operator Expr(int value) => new Constant(value);
    public static implicit operator Expr(long value) => new Constant(new Rational(value));
    public static implicit operator Expr(Rational value) => new Constant(value);

    /// <summary>
    /// Converts through the value's shortest decimal form, so 0.1 becomes exactly 1/10
    /// (the value the author wrote, not the nearest binary double). Throws
    /// <see cref="ArgumentException"/> for NaN and infinities, which have no exact value.
    /// </summary>
    public static implicit operator Expr(double value) => new Constant(value);

    public bool Equals(Expr? other) => other is not null && StructurallyEquals(this, other);
    public override bool Equals(object? obj) => obj is Expr other && Equals(other);

    public override int GetHashCode() => StructuralHash(this);

    private static bool StructurallyEquals(Expr a, Expr b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a.GetType() != b.GetType() || !a.PayloadEquals(b)) return false;

        ImmutableArray<Expr> left = a.Children, right = b.Children;
        if (left.Length != right.Length) return false;

        for (int i = 0; i < left.Length; i++)
            if (!left[i].Equals(right[i]))
                return false;

        return true;
    }

    private static int StructuralHash(Expr expr)
    {
        var hash = new HashCode();
        hash.Add(expr.GetType());
        hash.Add(expr.PayloadHashCode());
        foreach (Expr child in expr.Children)
            hash.Add(child);
        return hash.ToHashCode();
    }
}

public sealed class Constant : Expr
{
    public Rational Value { get; }

    public Constant(Rational value) => Value = value;
    public Constant(double value) : this(Rational.FromDouble(value)) { }
    public Constant(int value) : this(new Rational(value)) { }

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Value.ToDouble();
    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings) => new Complex(Value.ToDouble());
    protected override Expr DifferentiateCore(string variable) => new Constant(0);

    public override ImmutableArray<Expr> Children => NoChildren;
    public override Expr WithChildren(IReadOnlyList<Expr> children) => WithNoChildren(children);

    protected override bool PayloadEquals(Expr other) => Value == ((Constant)other).Value;
    protected override int PayloadHashCode() => Value.GetHashCode();
    protected internal override int ComparePayload(Expr other) => Value.CompareTo(((Constant)other).Value);

    public override string ToString() => Value.ToString();
}

public sealed class Variable(string name) : Expr
{
    public string Name { get; } = name;

    public override double Evaluate(IReadOnlyDictionary<string, double> bindings)
    {
        if (!bindings.TryGetValue(Name, out double value))
            throw new ArgumentException($"No binding provided for variable '{Name}'.");

        return value;
    }

    public override Complex EvaluateComplex(IReadOnlyDictionary<string, Complex> bindings)
    {
        if (!bindings.TryGetValue(Name, out Complex value))
            throw new ArgumentException($"No binding provided for variable '{Name}'.");

        return value;
    }

    protected override Expr DifferentiateCore(string variable) => new Constant(variable == Name ? 1 : 0);

    public override ImmutableArray<Expr> Children => NoChildren;
    public override Expr WithChildren(IReadOnlyList<Expr> children) => WithNoChildren(children);

    protected override bool PayloadEquals(Expr other) => Name == ((Variable)other).Name;
    protected override int PayloadHashCode() => Name.GetHashCode();
    protected internal override int ComparePayload(Expr other) => string.CompareOrdinal(Name, ((Variable)other).Name);

    public override IReadOnlySet<string> GetVariables() => new HashSet<string> { Name };

    public override Expr Substitute(string variable, Expr replacement) => variable == Name ? replacement : this;

    public override string ToString() => Name;
}