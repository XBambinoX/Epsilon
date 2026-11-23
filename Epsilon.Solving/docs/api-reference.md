# API reference

Every public type and member of the `Epsilon.Solving` package, version 1.0.0.
All types are in the namespace `Epsilon.Solving`. Each entry has a one-line description; the
[guides](README.md) explain the details, and your IDE shows the full XML documentation for every
member.

A test (`SolvingApiReferenceDocTests`) checks that every public type and member is listed
here.

**Contents:**
[Equations](#equations) -
[Exact solutions](#exact-solutions) -
[Numeric solutions](#numeric-solutions)

The conditions on solutions are the core's `Condition`, with `Left`, `Comparison` and `Right`,
and `Comparison` its enum: `Equal`, `NotEqual`, `Less`, `LessOrEqual`, `Greater`,
`GreaterOrEqual`. The overloads without a variable solve for the only variable of the equation
(x for one without any), and throw `InvalidOperationException` for more than one.

---

## Equations

### `Equation` (sealed class)

An equation, left = right, to solve for one of its variables.

| Member | Description |
|---|---|
| `Equation(Expr left, Expr right)` | The equation left = right. |
| `Expr Left` | The left side. |
| `Expr Right` | The right side. |
| `static Equation Parse(string input, params string[] variableNames)` | Two expressions in the syntax of `ExprParser.Parse` with `=` between them. Throws `FormatException` without `=`, with two, or for a malformed side, with positions counted from the start of the whole input. |
| `SolutionSet Solve()` / `Solve(string variable, Assumptions? assumptions = null)` | The real solutions, exact. The other variables are parameters; an assumption about the sign of the unknown leaves out the solutions that don't meet it. |
| `SolutionSet SolveComplex()` / `SolveComplex(string variable)` | The complex solutions, exact, of polynomial and rational equations; other functions of the unknown are left unsolved. |
| `NumericSolutionSet<double> SolveNumerically()` / `SolveNumerically(string variable, Assumptions? assumptions = null)` | The real solutions as numbers: the exact ones evaluated, and every real root of a polynomial without a formula. Families stay unsolved. |
| `NumericSolutionSet<double> SolveNumerically(double from, double to)` / `SolveNumerically(string variable, double from, double to, Assumptions? assumptions = null)` | The real solutions in [from, to], the limits possibly infinite: those above, the values of each family there, and the solutions of the rest, found by a scan. Throws `ArgumentException` for a NaN limit or from not below to. |
| `double? FindRoot(double near)` / `FindRoot(string variable, double near)` | A real solution by Newton's method from the guess, or null where it doesn't converge. |
| `NumericSolutionSet<ComplexNumber> SolveComplexNumerically()` / `SolveComplexNumerically(string variable)` | The complex solutions as numbers: the exact ones evaluated, and every root of a polynomial without a formula. |
| `NumericSolutionSet<ComplexNumber> SolveComplexNumerically(ComplexNumber from, ComplexNumber to)` / `SolveComplexNumerically(string variable, ComplexNumber from, ComplexNumber to)` | The complex solutions in the rectangle with the lower left and upper right corners given, those of the rest found by Newton's method from a grid. Throws `ArgumentException` for a corner that isn't finite or out of order. |
| `ComplexNumber? FindComplexRoot(ComplexNumber near)` / `FindComplexRoot(string variable, ComplexNumber near)` | A complex solution by Newton's method from the guess, or null. |
| `string Print()` | The equation as text: `sin(x) = 1 / 2`. |
| `string ToLatex()` | The equation in LaTeX. |

The numeric methods throw `InvalidOperationException` for an equation with a variable other than
the unknown.

## Exact solutions

### `SolutionSet` (sealed class)

The solutions of an equation in one unknown: exact values and families of them, each with the
conditions it holds under, or every value; and the parts that weren't solved.

| Member | Description |
|---|---|
| `string Variable` | The unknown. |
| `IReadOnlyList<Solution> Solutions` | The solutions, each once: numbers in ascending order (complex ones by the real part, then the imaginary part, families by their value at index 0), others in the order found. |
| `bool IsEveryValue` | Whether every value of the unknown is a solution where the equation is defined; `Solutions` is empty then. |
| `IReadOnlyList<Condition> Conditions` | With `IsEveryValue`, where every value is a solution: `x != 0` for x/x = 1, `a = b` for a = b. Empty otherwise. |
| `IReadOnlyList<Expr> Unsolved` | The expressions that must be 0 for the solutions not found; empty if every solution is listed. |
| `bool IsComplete` | Whether nothing is `Unsolved`. |
| `bool IsEmpty` | Whether the equation is known to have no solution. |
| `SolutionSet Within(Expr from, Expr to)` | The solutions in [from, to], the values of each family there one by one; more than ten thousand stay a family with bounds on its index. Throws `ArgumentException` for an end that isn't a finite number or from not below to, `InvalidOperationException` for complex solutions. |
| `string Print()` | `{2, 3}`, `{-b / a} if a != 0`, `{}`, `(-inf, inf)` (`C` over the complex numbers), `{1, ...}`, `{π * k | k in Z, k != 0}`. |
| `string ToLatex()` | The set in LaTeX, `\emptyset` for no solution. |

### `Solution` (sealed class)

One solution, an exact value or a family of them, and the conditions it holds under.

| Member | Description |
|---|---|
| `Expr Value` | The value, with the parameters and, for a family, the index in it: `2 * π * k + π / 6`. |
| `Expr? Period` | For a family, the distance between its values: `2 * π`, positive where it is a number. Null for a single value. |
| `string? Index` | For a family, the name of the integer in `Value`: `k`. Null for a single value. |
| `IReadOnlyList<Condition> Conditions` | What the value needs to be a solution, on the parameters and the index; empty if it always is. |
| `Solution? At(int index)` | A family's value at one index, with the conditions left on the parameters; null where the conditions on the index leave it out. A single value is itself. |
| `string Print()` | The value, a family's start first, and its conditions after `if`: `π / 6 + 2 * π * k`, `-b / a if a != 0`. |
| `string ToLatex()` | The same in LaTeX. |

## Numeric solutions

### `NumericSolutionSet<T>` (sealed class)

The solutions of an equation in one unknown as numbers: `double` over the real numbers,
`ComplexNumber` over the complex ones.

| Member | Description |
|---|---|
| `string Variable` | The unknown. |
| `IReadOnlyList<T> Values` | The solutions, each once, in ascending order (complex ones by the real part, then the imaginary part), to about full double precision. |
| `bool IsEveryValue` | Whether every value of the unknown is a solution where the equation is defined. |
| `IReadOnlyList<Condition> Conditions` | With `IsEveryValue`, where every value is a solution. |
| `IReadOnlyList<Expr> Unsolved` | The expressions that must be 0 for the solutions not found. |
| `bool IsComplete` | Whether nothing is `Unsolved`. |
| `bool IsEmpty` | Whether the equation is known to have no solution. |
| `string Print()` | `{1.1673039782614187}`, `{}`, `(-inf, inf)` (`C` over the complex numbers), `{...}`. |
