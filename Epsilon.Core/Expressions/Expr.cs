namespace Epsilon.Core;

/// <summary>
/// An immutable symbolic expression tree: numbers, variables, constants such as pi,
/// arithmetic and functions. Build one with <see cref="ExprParser.Parse"/> or with the C#
/// operators (<c>2 * x + 1</c>), then evaluate, differentiate, simplify or print it.
/// </summary>
/// <remarks>
/// Expressions compare by structure: built with operators, <c>x + 1</c> equals another <c>x + 1</c> but not
/// <c>1 + x</c> until both are simplified or canonicalized.
/// </remarks>
public abstract class Expr : IEquatable<Expr>
{
    /// <summary>Evaluates the expression over the reals with the given variable values.</summary>
    /// <param name="bindings">A value for every variable in the expression, by name.</param>
    /// <returns>
    /// The value as a <see cref="double"/>; NaN or an infinity where the expression is undefined
    /// over the reals (sqrt(-1), ln(0), 1/0).
    /// </returns>
    /// <exception cref="ArgumentException">A variable has no binding.</exception>
    public abstract double Evaluate(IReadOnlyDictionary<string, double> bindings);

    /// <summary>
    /// Evaluates an expression of at most one variable, binding that variable to <paramref name="x"/>.
    /// For an expression without variables (<c>2*pi</c>) the argument is ignored.
    /// </summary>
    /// <exception cref="InvalidOperationException">The expression has two or more variables.</exception>
    public double Evaluate(double x) => Evaluate(SingleBinding(x));

    /// <summary>Evaluates with the variable values given as pairs: <c>expr.Evaluate(("x", 1), ("y", 2))</c>.</summary>
    /// <exception cref="ArgumentException">A variable has no binding.</exception>
    public double Evaluate(params (string Name, double Value)[] bindings)
    {
        var dict = new Dictionary<string, double>(bindings.Length);
        foreach (var (name, value) in bindings)
            dict[name] = value;

        return Evaluate(dict);
    }

    // Simplified once here, at the top, in the default Generic mode - the same result as
    // simplifying inside every node, as this used to do for some nodes but not others.
    // (Strict would keep leftovers such as x/x in d/dx x^x.) The nodes build their
    // derivatives from DerivativeOf, which doesn't simplify.
    /// <summary>
    /// The symbolic partial derivative with respect to <paramref name="variable"/>, simplified
    /// (<see cref="SimplifyMode.Generic"/>). Exactly 0 when the expression does not depend on it.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// The expression contains a function whose derivative is not supported yet: floor, ceiling,
    /// round, sign, min, max, or nthroot with a degree that depends on the variable.
    /// </exception>
    public Expr Differentiate(string variable) =>
        RawDerivative(variable).Simplify();

    // The derivative of anything that doesn't depend on `variable` is exactly 0.
    // Checked here once instead of in every node, so rules like the quotient rule
    // never produce leftovers such as 0/y (which Simplify won't fold unless y is
    // provably nonzero).
    private Expr RawDerivative(string variable) =>
        DependsOn(variable) ? DifferentiateCore(variable) : new Constant(0);

    /// <summary>
    /// The derivative of this node with respect to <paramref name="variable"/>, built from the
    /// derivatives of its children via <see cref="DerivativeOf"/>, without simplifying.
    /// Only called when the node actually depends on the variable, so implementations need
    /// not handle the constant case.
    /// </summary>
    protected abstract Expr DifferentiateCore(string variable);

    /// <summary>
    /// The unsimplified derivative of <paramref name="child"/>, for use inside
    /// <see cref="DifferentiateCore"/>. <see cref="Differentiate(string)"/> simplifies the
    /// whole result once at the end.
    /// </summary>
    protected static Expr DerivativeOf(Expr child, string variable) => child.RawDerivative(variable);

    /// <summary>
    /// The derivative with respect to the expression's only variable; 0 for an expression without variables.
    /// </summary>
    /// <exception cref="InvalidOperationException">The expression has two or more variables.</exception>
    /// <exception cref="NotSupportedException">
    /// The expression contains a function whose derivative is not supported yet: floor, ceiling,
    /// round, sign, min, max, or nthroot with a degree that depends on the variable.
    /// </exception>
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

    /// <summary>
    /// Evaluates the expression over the complex numbers, using principal branches
    /// (sqrt(-1) = i, ln(-1) = pi*i).
    /// </summary>
    /// <param name="bindings">A value for every variable in the expression, by name.</param>
    /// <exception cref="ArgumentException">A variable has no binding.</exception>
    /// <exception cref="NotSupportedException">
    /// The expression contains a function without complex evaluation: floor, ceiling, round and
    /// sign (planned for a future version), or min and max (the complex numbers are not ordered).
    /// </exception>
    public virtual ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) =>
        throw new NotSupportedException($"{GetType().Name} does not support complex evaluation.");

    /// <summary>
    /// Complex evaluation of an expression of at most one variable, binding it to <paramref name="x"/>.
    /// For an expression without variables the argument is ignored.
    /// </summary>
    /// <exception cref="InvalidOperationException">The expression has two or more variables.</exception>
    public ComplexNumber EvaluateComplex(ComplexNumber x) => EvaluateComplex(SingleBinding(x));

    /// <summary>Complex evaluation with the variable values given as pairs.</summary>
    /// <exception cref="ArgumentException">A variable has no binding.</exception>
    public ComplexNumber EvaluateComplex(params (string Name, ComplexNumber Value)[] bindings)
    {
        var dict = new Dictionary<string, ComplexNumber>(bindings.Length);
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
    /// <summary>
    /// Compares the node's non-child data with <paramref name="other"/>, which is always of the same
    /// type. Override in nodes that carry data besides their children.
    /// </summary>
    protected virtual bool PayloadEquals(Expr other) => true;
    /// <summary>Hash of the node's non-child data, consistent with <see cref="PayloadEquals"/>.</summary>
    protected virtual int PayloadHashCode() => 0;
    /// <summary>
    /// Orders the node's non-child data against <paramref name="other"/> (same type), for the
    /// canonical term order.
    /// </summary>
    protected internal virtual int ComparePayload(Expr other) => 0;

    /// <summary>The <see cref="Children"/> of a leaf node.</summary>
    protected static readonly ImmutableArray<Expr> NoChildren = [];
    private static readonly IReadOnlySet<string> NoVariables = new HashSet<string>();

    // Shared WithChildren implementations for leaves and two-child nodes.
    /// <summary><see cref="WithChildren"/> for leaf nodes: returns this node, or throws if children are given.</summary>
    protected Expr WithNoChildren(IReadOnlyList<Expr> children) =>
        children.Count == 0 ? this : throw ChildCountMismatch(0, children.Count);

    /// <summary>
    /// <see cref="WithChildren"/> for two-child nodes: returns this node if both children are
    /// unchanged, otherwise calls <paramref name="create"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Not exactly two children are given.</exception>
    protected Expr WithTwoChildren(IReadOnlyList<Expr> children, Func<Expr, Expr, Expr> create)
    {
        if (children.Count != 2)
            throw ChildCountMismatch(2, children.Count);

        ImmutableArray<Expr> current = Children;
        return ReferenceEquals(children[0], current[0]) && ReferenceEquals(children[1], current[1])
            ? this
            : create(children[0], children[1]);
    }

    /// <summary>The exception to throw from <see cref="WithChildren"/> for a wrong number of children.</summary>
    protected ArgumentException ChildCountMismatch(int expected, int actual) =>
        new($"{GetType().Name} has {expected} children, got {actual}.", "children");

    /// <summary>The names of all variables that occur in the expression.</summary>
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

    /// <summary>Whether <paramref name="variable"/> occurs in the expression.</summary>
    public virtual bool DependsOn(string variable) => GetVariables().Contains(variable);

    /// <summary>
    /// Replaces every occurrence of <paramref name="variable"/> with <paramref name="replacement"/>.
    /// The result is not simplified.
    /// </summary>
    public virtual Expr Substitute(string variable, Expr replacement) =>
        MapChildren(child => child.Substitute(variable, replacement));

    /// <summary>The name of the expression's only variable.</summary>
    /// <exception cref="InvalidOperationException">The expression does not have exactly one variable.</exception>
    public string GetSingleVariable()
    {
        var vars = GetVariables();

        if (vars.Count != 1)
            throw new InvalidOperationException(
                $"Expected exactly 1 variable, found {vars.Count}: [{string.Join(", ", vars)}]. " +
                "Use the explicit-variable overload for multivariable expressions.");

        return vars.First();
    }

    // Binding for the single-argument Evaluate overloads. A constant expression has no
    // variable to bind, so the argument is simply ignored (like Differentiate() returning 0),
    // instead of throwing on e.g. Parse("2*pi").Evaluate(0).
    private IReadOnlyDictionary<string, T> SingleBinding<T>(T value) =>
        GetVariables().Count == 0
            ? new Dictionary<string, T>()
            : new Dictionary<string, T> { [GetSingleVariable()] = value };

    // Operators
    //
    // Build expressions with ordinary C# syntax: x * x + 2 * x - 0.5. They only construct
    // nodes (x + 0 stays Add(x, 0)); call Simplify() to clean the result up.
    //
    // There is deliberately no ^ operator: in C# it is XOR and binds looser than +, so
    // x ^ 2 + 1 would silently mean x ^ (2 + 1). Use x.Pow(2) instead.

    /// <summary>Builds <c>left + right</c> without simplifying.</summary>
    public static Expr operator +(Expr left, Expr right) => new Add(left, right);
    /// <summary>Builds <c>left - right</c> without simplifying.</summary>
    public static Expr operator -(Expr left, Expr right) => new Subtract(left, right);
    /// <summary>Builds <c>left * right</c> without simplifying.</summary>
    public static Expr operator *(Expr left, Expr right) => new Multiply(left, right);
    /// <summary>Builds <c>left / right</c> without simplifying.</summary>
    public static Expr operator /(Expr left, Expr right) => new Divide(left, right);
    /// <summary>Builds <c>-argument</c> without simplifying.</summary>
    public static Expr operator -(Expr argument) => new Negate(argument);

    /// <summary>This expression raised to <paramref name="exponent"/>: x.Pow(2) is x^2.</summary>
    public Expr Pow(Expr exponent) => new Power(this, exponent);

    /// <summary>An exact integer constant, so numbers mix with expressions: <c>2 * x</c>.</summary>
    public static implicit operator Expr(int value) => new Constant(value);
    /// <summary>An exact integer constant.</summary>
    public static implicit operator Expr(long value) => new Constant(new Rational(value));
    /// <summary>An exact rational constant.</summary>
    public static implicit operator Expr(Rational value) => new Constant(value);

    /// <summary>
    /// Converts through the value's shortest decimal form, so 0.1 becomes exactly 1/10
    /// (the value the author wrote, not the nearest binary double). Throws
    /// <see cref="ArgumentException"/> for NaN and infinities, which have no exact value.
    /// </summary>
    public static implicit operator Expr(double value) => new Constant(value);

    /// <summary>
    /// Structural equality: same node types, same data and equal children in the same order.
    /// Mathematically equal but differently built expressions (<c>x + 1</c>, <c>1 + x</c>) are not equal until canonicalized.
    /// </summary>
    public bool Equals(Expr? other) => other is not null && StructurallyEquals(this, other);
    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Expr other && Equals(other);

    /// <inheritdoc/>
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

/// <summary>An exact rational number, such as 2, -3/4 or 0.1 (stored exactly as 1/10).</summary>
public sealed class Constant : Expr
{
    /// <summary>The exact value.</summary>
    public Rational Value { get; }

    /// <summary>A constant with the exact value <paramref name="value"/>.</summary>
    public Constant(Rational value) => Value = value;
    /// <summary>
    /// A constant from the shortest decimal form of <paramref name="value"/>: 0.1 becomes exactly 1/10.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is NaN or infinite.</exception>
    public Constant(double value) : this(Rational.FromDouble(value)) { }
    /// <summary>An integer constant.</summary>
    public Constant(int value) : this(new Rational(value)) { }
    // Without this, new Constant(5L) is ambiguous between the Rational and double overloads.
    /// <summary>An integer constant.</summary>
    public Constant(long value) : this(new Rational(value)) { }

    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) => Value.ToDouble();
    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings) => new ComplexNumber(Value.ToDouble());
    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => new Constant(0);

    /// <inheritdoc/>
    public override ImmutableArray<Expr> Children => NoChildren;
    /// <inheritdoc/>
    public override Expr WithChildren(IReadOnlyList<Expr> children) => WithNoChildren(children);

    /// <inheritdoc/>
    protected override bool PayloadEquals(Expr other) => Value == ((Constant)other).Value;
    /// <inheritdoc/>
    protected override int PayloadHashCode() => Value.GetHashCode();
    /// <inheritdoc/>
    protected internal override int ComparePayload(Expr other) => Value.CompareTo(((Constant)other).Value);

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}

/// <summary>A named variable, such as x. Its value comes from the bindings passed to Evaluate.</summary>
/// <param name="name">The variable's name.</param>
public sealed class Variable(string name) : Expr
{
    /// <summary>The variable's name.</summary>
    public string Name { get; } = name;

    /// <inheritdoc/>
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings)
    {
        if (!bindings.TryGetValue(Name, out double value))
            throw new ArgumentException($"No binding provided for variable '{Name}'.");

        return value;
    }

    /// <inheritdoc/>
    public override ComplexNumber EvaluateComplex(IReadOnlyDictionary<string, ComplexNumber> bindings)
    {
        if (!bindings.TryGetValue(Name, out ComplexNumber value))
            throw new ArgumentException($"No binding provided for variable '{Name}'.");

        return value;
    }

    /// <inheritdoc/>
    protected override Expr DifferentiateCore(string variable) => new Constant(variable == Name ? 1 : 0);

    /// <inheritdoc/>
    public override ImmutableArray<Expr> Children => NoChildren;
    /// <inheritdoc/>
    public override Expr WithChildren(IReadOnlyList<Expr> children) => WithNoChildren(children);

    /// <inheritdoc/>
    protected override bool PayloadEquals(Expr other) => Name == ((Variable)other).Name;
    /// <inheritdoc/>
    protected override int PayloadHashCode() => Name.GetHashCode();
    /// <inheritdoc/>
    protected internal override int ComparePayload(Expr other) => string.CompareOrdinal(Name, ((Variable)other).Name);

    /// <inheritdoc/>
    public override IReadOnlySet<string> GetVariables() => new HashSet<string> { Name };

    /// <inheritdoc/>
    public override Expr Substitute(string variable, Expr replacement) => variable == Name ? replacement : this;

    /// <inheritdoc/>
    public override string ToString() => Name;
}