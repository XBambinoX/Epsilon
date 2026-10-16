# Gradients

The gradient of a function of several variables is the vector of its partial derivatives.
`Gradient` gives it as a `Vector<Expr>` of simplified expressions, and numbers come from
evaluating or compiling that vector.

## The gradient

```csharp
var f = ExprParser.Parse("x^2*y + sin(y)");

f.Gradient("x", "y").Print();   // [2x * y, x^2 + cos(y)]
f.Gradient("y", "x").Print();   // [x^2 + cos(y), 2x * y]
f.Gradient("x", "z").Print();   // [2x * y, 0]
```

The entries follow the order of the variables, and a variable the expression doesn't have gives
0. The variables can also be a list, such as `f.Gradient(["x", "y"])`. Without them the gradient
is by every variable of the expression, in ordinal order of the names (uppercase before
lowercase):

```csharp
f.Gradient().Print();   // [2x * y, x^2 + cos(y)]
```

Names longer than one letter must be declared when parsing. Otherwise `omega` is a product of
letters, with `e` read as Euler's number and `i` as the imaginary unit:

```csharp
var g = ExprParser.Parse("omega*sigma^2", "omega", "sigma");
g.Gradient("omega", "sigma").Print();   // [sigma^2, 2omega * sigma]

ExprParser.Parse("omega*sigma^2").Print();   // e * i * a^2 * a * g * g * m * m * o * s
```

## Numbers

`Evaluate` gives the gradient at a point, and `Compile` a delegate that computes it many times
faster, for use in a loop (see
[From formulas to fast code](../../Epsilon.LinearAlgebra/docs/fast-code.md)):

```csharp
var gradient = f.Gradient("x", "y");

gradient.Evaluate(("x", 1), ("y", 2));   // [4, 0.5838531634528576]
gradient.Compile("x", "y")(1, 2);        // [4, 0.5838531634528576]
```

## Gradient descent

Steps against the gradient go downhill. On (x - 1)^2 + 2(y + 2)^2, from the origin:

```csharp
var slope = ExprParser.Parse("(x - 1)^2 + 2*(y + 2)^2").Gradient("x", "y").Compile("x", "y");

Vector<double> p = [0.0, 0.0];
for (int i = 0; i < 100; i++)
    p -= 0.1 * slope(p[0], p[1]);

p;   // [0.9999999997962964, -1.9999999999999998]: the minimum is at (1, -2)
```

## Directional derivatives

The rate of change of a function along a direction is the gradient dotted with it:

```csharp
ExprParser.Parse("x^2*y").DirectionalDerivative([1, 1], "x", "y").Print();   // 2x * y + x^2
```

The direction isn't normalized. Along a unit vector the result is the rate per unit of length;
along a longer vector it is that rate times the length:

```csharp
f.DirectionalDerivative([0.6, 0.8], "x", "y").Evaluate(("x", 1), ("y", 2));   // 2.867082530762286
f.DirectionalDerivative([3, 4], "x", "y").Evaluate(("x", 1), ("y", 2));       // 14.335412653811431, 5 times as much
```

The direction can be symbolic, such as the unit vector at an angle t:

```csharp
f.DirectionalDerivative(Vector<Expr>.Parse("[cos(t), sin(t)]"), "x", "y").Print();
// 2cos(t) * x * y + (x^2 + cos(y)) * sin(t)
```

## Errors

| Call | Throws |
|---|---|
| `f.Gradient("x", "y", "x")` | `ArgumentException`: Variable 'x' is listed twice. |
| `f.DirectionalDerivative([1, 0, 0], "x", "y")` | `ArgumentException`: The direction has 3 entries but there are 2 variables. |
| `ExprParser.Parse("floor(x)*y").Gradient()` | `NotSupportedException`: floor, ceiling, round, sign, min and max have no derivatives yet |
