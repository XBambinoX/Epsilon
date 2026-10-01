# Expressions

Every formula in Epsilon is an `Expr`: an **immutable tree** of nodes. `x^2 + sin(x)` is an
`Add` node with a `Power` and a `Sin` below it. You get an `Expr` either by
[parsing text](parsing.md) or by building it in C#, as shown here.

## Building with operators

`Variable`, numbers and the C# operators `+ - * /` and unary `-` build trees directly.
Use `Pow` for powers — C#'s `^` is XOR and binds looser than `+`, so `x ^ 2 + 1` would
silently mean `x ^ (2 + 1)`:

```csharp
var x = new Variable("x");
var y = new Variable("y");

Expr e = 3 * x.Pow(2) - x / y + 1;
e.Print();   // 3x^2 - x / y + 1
```

Operators **only build nodes**; they never simplify. Call [`Simplify`](simplification.md)
when you want the result cleaned up:

```csharp
(x + 0).Print();              // x + 0
(x + 0).Simplify().Print();   // x
```

### Numbers

`int`, `long`, `Rational` and `double` convert to `Expr` implicitly. A `double` is converted
through its shortest decimal form, so `0.1` becomes exactly `1/10`, not the nearest binary
fraction. For values such as 1/3 that have no short decimal form, use `Rational`:

```csharp
(0.1 * x).Print();                   // (1/10) * x
(new Rational(1, 3) * x).Print();    // (1/3) * x
```

`double.NaN` and infinities have no exact value and throw `ArgumentException`. See
[Numbers](numbers.md).

## Node types

All node types are public, so you can also construct them directly:

| Kind | Types |
|---|---|
| Leaves | `Constant`, `Variable`, `Pi`, `EulerNumber`, `ImaginaryUnit` |
| Arithmetic | `Add`, `Subtract`, `Multiply`, `Divide`, `Power`, `Negate` |
| Trigonometric | `Sin`, `Cos`, `Tan`, `Cot`, `Sec`, `Csc`, `Asin`, `Acos`, `Atan` |
| Hyperbolic | `Sinh`, `Cosh`, `Tanh`, `Coth`, `Sech`, `Csch`, `Asinh`, `Acosh`, `Atanh` |
| Other functions | `Exp`, `Ln`, `Sqrt`, `NthRoot`, `Abs`, `Sign`, `Floor`, `Ceiling`, `Round`, `Min`, `Max` |

```csharp
Expr f = new Sin(x) + new Pi();
f.Print();   // sin(x) + π
```

There is no `Log` node: the parser turns `log(x, b)` into `ln(x) / ln(b)`.

## Inspecting a tree

`GetVariables` lists the variables, `DependsOn` checks for one:

```csharp
Expr g = ExprParser.Parse("x*y + sin(z)");

g.GetVariables();    // {x, y, z}
g.DependsOn("w");    // false
```

Every node exposes its direct sub-expressions as `Children`, and the binary and unary nodes
support positional patterns:

```csharp
Expr p = ExprParser.Parse("sin(x)^2");

if (p is Power(Sin(Variable v), Constant c))
    Console.WriteLine($"{v.Name}, {c.Value}");   // x, 2
```

### Transforming a tree

`MapChildren` applies a function to every child and rebuilds the node only if something
changed. Together with recursion that is all you need for a rewrite. This one replaces every
`sin` with `cos`:

```csharp
static Expr SinToCos(Expr e)
{
    Expr mapped = e.MapChildren(SinToCos);
    return mapped is Sin(var a) ? new Cos(a) : mapped;
}

SinToCos(ExprParser.Parse("sin(x) + 2sin(y)")).Print();   // 2cos(y) + cos(x)
```

`WithChildren` rebuilds a node of the same type with the children you give it.
Because trees are immutable, unchanged subtrees are shared rather than copied.

## Substitution

`Substitute` replaces a variable with any expression. The result is not simplified:

```csharp
Expr q = ExprParser.Parse("x^2 + 1");

q.Substitute("x", ExprParser.Parse("y + 1")).Print();   // (y + 1)^2 + 1
q.Substitute("x", 3).Simplify().Print();                // 10
```

## Equality

`Equals` and `GetHashCode` are **structural**: same node types, same values, same children
in the same order. Built with operators, `1 + x` and `x + 1` are different trees. `Parse`
and `Simplify` return canonical trees (sums and products in a fixed order), and
`Canonicalize` does only that ordering:

```csharp
(1 + x).Equals(x + 1);                                 // false
(1 + x).Canonicalize().Equals((x + 1).Canonicalize()); // true
ExprParser.Parse("1 + x").Equals(ExprParser.Parse("x + 1"));   // true
```

Structural equality is not mathematical equality — see
[limitations](limitations.md#equality-is-structural).

## Custom node types

You can add your own function by deriving from `UnaryExpr` (one argument) or `Expr`. A
node only implements its own math; traversal, equality, substitution and `Simplify` work
with it automatically. Build the derivative with `DerivativeOf`, which gives the child's
derivative unsimplified — `Differentiate` simplifies the whole result once:

```csharp
public sealed class Sigmoid(Expr argument) : UnaryExpr(argument)
{
    public override double Evaluate(IReadOnlyDictionary<string, double> bindings) =>
        1 / (1 + Math.Exp(-Argument.Evaluate(bindings)));

    // σ'(u) = σ(u) * (1 - σ(u)) * u'
    protected override Expr DifferentiateCore(string variable) =>
        this * (1 - this) * DerivativeOf(Argument, variable);

    protected override Expr WithArgument(Expr argument) => new Sigmoid(argument);

    public override string ToString() => $"sigmoid({Argument.Print()})";
}

var s = new Sigmoid(2 * x);
s.Evaluate(0);                        // 0.5
s.Differentiate("x").Print();         // 2 * (-sigmoid(2x) + 1) * sigmoid(2x)
```

`Print` and `ToLatex` fall back to `ToString` for unknown nodes, and the parser doesn't know
them. Override `EvaluateComplex` if the function has a complex extension; otherwise it
throws `NotSupportedException`. A node that stores data besides its children (like
`Constant.Value`) overrides `PayloadEquals`, `PayloadHashCode` and `ComparePayload`. That
data must not change after the node is built: a node is immutable like the rest of the
tree, and its hash is computed once and kept.
