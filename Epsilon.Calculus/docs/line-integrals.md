# Line integrals

A line integral sums a field along a `Curve`. For an expression it is the integral by arc
length, f ds: with f = 1 the length of the curve, with f a density the mass of a wire. For a
vector field, a `Vector<Expr>`, it is the integral along the direction of the curve, F . dr: the
work the field does along it, and around a closed curve its circulation. `LineIntegral` computes
it numerically, as a `double`, and `LineIntegralExact` exactly, as an `Expr`. `Potential` finds a
function whose gradient is a field.

**Contents:**
[Curves](#curves) -
[By arc length](#by-arc-length) -
[Along a vector field](#along-a-vector-field) -
[Potentials](#potentials) -
[Numeric and exact](#numeric-and-exact) -
[What isn't found](#what-isnt-found) -
[How it works](#how-it-works)

## Curves

A curve is a point whose entries are expressions of a parameter, for the parameter from one
value to another, in increasing order. `Reversed()` traverses it the other way. It may be made
of pieces: `Segment` is a straight segment, `Polygon` the segments through points, closed back to
the first with `closed: true`, and `Join` puts curves one after another:

```csharp
var t = new Variable("t");
var circle = new Curve("t", [new Cos(t), new Sin(t)], 0, 2 * new Pi());
var helix = new Curve("t", Vector<Expr>.Parse("[cos(t), sin(t), t]"), 0, 2 * new Pi());
var segment = Curve.Segment([0, 0], [1, 2]);
var triangle = Curve.Polygon([[0, 0], [1, 0], [0, 1]], closed: true);
var there = Curve.Join(segment, Curve.Segment([1, 2], [3, 2]));

circle.Start.Print();               // [1, 0]
there.End.Print();                  // [3, 2]
helix.Dimension;                    // 3
circle.Reversed().End.Print();      // [1, 0], clockwise
```

The variables a line integral takes name the entries of the curve's points, in order. The
parameter may share a name with one of them, as x does in the graph of x^2 by x.

## By arc length

The integral of f by arc length doesn't depend on the direction the curve is traversed in:

```csharp
ExprParser.Parse("1").LineIntegralExact(circle, "x", "y").Print();           // 2 * π
ExprParser.Parse("1").LineIntegralExact(helix, "x", "y", "z").Print();       // 2 * π * sqrt(2)
ExprParser.Parse("1").LineIntegralExact(triangle, "x", "y").Print();         // sqrt(2) + 2
ExprParser.Parse("x^2 + y^2").LineIntegralExact(circle, "x", "y").Print();   // 2 * π

var graph = new Curve("x", Vector<Expr>.Parse("[x, x^2]"), 0, 1);
ExprParser.Parse("x").LineIntegralExact(graph, "x", "y").Print();            // (1/12) * (5sqrt(5) - 1)

var catenary = new Curve("t", Vector<Expr>.Parse("[t, cosh(t)]"), 0, 1);
ExprParser.Parse("1").LineIntegralExact(catenary, "x", "y").Print();         // sinh(1)
```

Other variables are constants, in the curve and in the integrand:

```csharp
var circleOfRadius = new Curve("t", Vector<Expr>.Parse("[R*cos(t), R*sin(t)]"), 0, 2 * new Pi());
ExprParser.Parse("1").LineIntegralExact(circleOfRadius, "x", "y").Print();   // 2abs(R) * π
```

## Along a vector field

Entry i of the field is its component along the i-th variable. Traversed the other way, the
integral changes sign:

```csharp
var swirl = Vector<Expr>.Parse("[-y, x]");
var square = Curve.Polygon([[0, 0], [1, 0], [1, 1], [0, 1]], closed: true);

swirl.LineIntegralExact(circle, "x", "y").Print();              // 2 * π
swirl.LineIntegralExact(circle.Reversed(), "x", "y").Print();   // -2 * π
swirl.LineIntegralExact(square, "x", "y").Print();              // 2
Vector<Expr>.Parse("[-y, x, z]").LineIntegralExact(helix, "x", "y", "z").Print();   // 2 * π + 2π^2
```

By Green's theorem the circulation of [-y, x] around a closed curve, counterclockwise, is twice
the area inside it: 2 pi for the unit disk and 2 for the unit square.

## Potentials

A field is a gradient where the derivative of entry i by variable j is that of entry j by
variable i, and `Potential` then finds a function whose gradient it is. The work of a gradient
along a curve is the difference of its potential between the ends, whichever way it goes:

```csharp
var field = Vector<Expr>.Parse("[2*x*y, x^2]");

field.Potential("x", "y").Print();                                      // x^2 * y
field.LineIntegralExact(segment, "x", "y").Print();                     // 2, which is 1^2 * 2 - 0
Vector<Expr>.Parse("[y*z, x*z, x*y]").Potential("x", "y", "z").Print();  // x * y * z

swirl.Potential("x", "y");
// InvalidOperationException: The field [-y, x] isn't a gradient: the derivative of -y by y isn't that of x by x.
```

The potential holds where the field is defined around the curve. [-y, x]/(x^2 + y^2) has equal
derivatives, but it isn't defined at 0, and around the unit circle its work is 2 pi, not 0:

```csharp
Vector<Expr>.Parse("[-y/(x^2 + y^2), x/(x^2 + y^2)]").LineIntegralExact(circle, "x", "y").Print();   // 2 * π
```

## Numeric and exact

`LineIntegral` integrates numerically, as [numeric integration](numeric-integration.md) does,
and `LineIntegralExact` exactly, as [exact definite integrals](exact-integration.md) do. The
length of an ellipse is an elliptic integral, which has no exact value here:

```csharp
var ellipse = new Curve("t", Vector<Expr>.Parse("[2*cos(t), sin(t)]"), 0, 2 * new Pi());

ExprParser.Parse("1").LineIntegral(ellipse, "x", "y");        // 9.688448220547672
ExprParser.Parse("1").LineIntegralExact(ellipse, "x", "y");
// NotSupportedException: The line integral isn't found: the integral of sqrt(3sin(t)^2 + 1) by t from 0 to 2 * π isn't found.
```

The numeric integral needs numbers: a curve or an integrand with another variable throws
`ArgumentException`.

## What isn't found

| Integral | Why |
|---|---|
| `1` by arc length around an ellipse, exact | the length is an elliptic integral; `LineIntegral` gives the number |
| the potential of `[-y/(x^2 + y^2), x/(x^2 + y^2)]` | 1/(x^2 + y^2) isn't integrated by x with y a symbol |
| the potential of `[a*y, x]` | whether a is 1, which would make it a gradient, isn't decided |

## How it works

On each piece of the curve, the variables are replaced by the entries of r(t). By arc length the
integrand is f(r(t)) sqrt(r'(t) . r'(t)), with c sin(u)^2 + c cos(u)^2 written as c and
c + c sinh(u)^2 as c cosh(u)^2, which the simplifier sees only for numbers c; along a field it is
F(r(t)) . r'(t). It is integrated over the piece's interval, numerically or exactly, and the
pieces are added up. `Potential` compares the derivatives of the entries two by two, by
simplifying their difference or, where that doesn't show 0, by its values at a few points, then
integrates entry 0 by the first variable, adds the integral of what entry 1 adds by the second,
and so on.
