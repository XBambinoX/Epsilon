# Parameters

## Other variables

`Solve("x")` solves for x, and the other variables are parameters: real numbers the solutions
may contain. A solution that needs something of them comes with the conditions it holds under,
in its `Conditions`:

```csharp
Equation.Parse("x + a = 0").Solve("x").Print();     // {-a}
Equation.Parse("a*x = b").Solve("x").Print();       // {b / a} if a != 0
Equation.Parse("x^2 = a").Solve("x").Print();       // {-sqrt(a), sqrt(a)} if a >= 0

Condition condition = Equation.Parse("x^2 = a").Solve("x").Solutions[0].Conditions[0];
condition.Left.Print();    // a
condition.Comparison;      // GreaterOrEqual
condition.Right.Print();   // 0
```

The conditions are those of the general solution, under which it is right. What happens where
they fail isn't worked out: a x = b at a = 0 has no solution, or every x is one, as b decides,
and the set says neither.

## Formulas

A formula is solved for one of its variables like any equation:

```csharp
Equation.Parse("E = m*v^2/2").Solve("v").Print();
// {-sqrt(2E / m), sqrt(2E / m)} if m != 0 and E / m >= 0
Equation.Parse("T = 2*pi*sqrt(L/g)").Solve("g").Print();    // {4π^2 * L / T^2} if T > 0 and L != 0
Equation.Parse("1/x + 1/y = 1/f").Solve("x").Print();       // {-f * y / (f - y)} if f != y and f != 0 and y != 0
Equation.Parse("A = P*(1 + r)^n").Solve("n").Print();
// {ln(A / P) / ln(r + 1)} if r != 0 and P != 0 and A / P > 0 and r > -1
```

## Quadratics

A quadratic with parameters gives the formula, with the conditions for a real root. A
discriminant that is a square needs no absolute value, and the roots come out plain:

```csharp
Equation.Parse("a*x^2 + b*x + c = 0").Solve("x").Print();
// {(-b - sqrt(b^2 - 4a * c)) / (2a), (-b + sqrt(b^2 - 4a * c)) / (2a)} if a != 0 and b^2 - 4a * c >= 0
Equation.Parse("x^2 - 2*a*x + 1 = 0").Solve("x").Print();
// {a - sqrt(a^2 - 1), a + sqrt(a^2 - 1)} if abs(a) >= 1
Equation.Parse("x^2 - (a + b)*x + a*b = 0").Solve("x").Print();   // {b, a}
```

## Functions with parameters

Functions are undone with parameters too, periodic ones into families whose period has the
parameters in it, of either sign:

```csharp
Equation.Parse("exp(a*x) = b").Solve("x").Print();     // {ln(b) / a} if a != 0 and b > 0
Equation.Parse("cos(x) = 1/a").Solve("x").Print();
// {acos(1 / a) + 2 * π * k, -acos(1 / a) + 2 * π * k | k in Z} if abs(a) >= 1
Equation.Parse("sin(a*x) = 1/2").Solve("x").Print();
// {π / (6a) + 2 * π * k / a, 5 * π / (6a) + 2 * π * k / a | k in Z} if a != 0
```

## The form of the conditions

The conditions are put as simply as the solver finds them: bounds for roots and absolute values,
without squares, which are never negative, and without what other conditions imply:

```csharp
Equation.Parse("sqrt(x) + sqrt(x + a) = 1").Solve("x").Print();   // {(a - 1)^2 / 4} if a >= -1 and a <= 1
Equation.Parse("x^2 + a^2 = 0").Solve("x").Print();               // {0} if a = 0
```

## Assumptions

Assumptions about the parameters decide their conditions, and an assumption that contradicts
one leaves the solution out:

```csharp
var positive = Assumptions.None.AssumePositive("a");

Equation.Parse("a*x = b").Solve("x", positive).Print();                                  // {b / a}
Equation.Parse("x^2 = a").Solve("x", positive).Print();                                  // {-sqrt(a), sqrt(a)}
Equation.Parse("x^2 = a").Solve("x", Assumptions.None.AssumeNegative("a")).Print();      // {}
```
