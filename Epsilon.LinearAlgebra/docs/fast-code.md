# From formulas to fast code

Derive a formula once with symbols, then evaluate it as often as needed as fast as
handwritten code: compile it to a delegate, or write it out as C# for a project without
Epsilon.

## Compiled evaluation

```csharp
Func<double, Matrix<double>> Compile()                   // entries with at most one variable
Func<double, Matrix<double>> Compile(string x)           // also (x, y) and (x, y, z)
Func<double[], Matrix<double>> Compile(IReadOnlyList<string> variables)
Action<double[], double[]> CompileEntries(IReadOnlyList<string> variables)
```

The same for vectors, which give `Vector<double>`:

```csharp
var helix = Vector<Expr>.Parse("[cos(t), sin(t), t / 10]", ["t"]);
Func<double, Vector<double>> point = helix.Compile();
point(Math.PI);              // [-1, 1.2246467991473532E-16, 0.3141592653589793]

var table = Matrix<Expr>.Parse("[[x + y, x * y], [x - y, x / y]]");
table.Compile("x", "y")(6, 3);   // [[9, 18], [3, 2]]

var swirl = Vector<Expr>.Parse("[-y, x, z]");
swirl.Compile("x", "y", "z")(1, 2, 3);   // [-2, 1, 3]
```

The delegate returns exactly what `Evaluate` returns, bit for bit, and compiles all entries
together: a subexpression that occurs in several of them, such as `cos(t)` in a rotation, is
computed once. Like `Evaluate` it doesn't simplify. Every call returns a new matrix, and the
delegate can be called from many threads at once. For loops that must not allocate,
`CompileEntries` writes into an array that you give it and may reuse, entry (i, j) at index
`i * Columns + j`:

```csharp
var entries = new double[4];
table.CompileEntries(["x", "y"])([6, 3], entries);   // entries: [9, 18, 3, 2]
```

Compiling takes about a millisecond. In a quick measurement a 3 x 3 rotation by three angles
took about 2 microseconds with `Evaluate`, 60 nanoseconds compiled and 30 with
`CompileEntries`. The errors are those of the core's
[`Expr.Compile`](../../Epsilon.Core/docs/evaluation.md#compiled-evaluation): a variable that
isn't listed throws `ArgumentException` when compiling.

## Jacobian

The Jacobian of a vector of expressions is the matrix of its partial derivatives: one row per
entry and one column per variable, in the order you give:

```csharp
var polar = Vector<Expr>.Parse("[r * cos(t), r * sin(t)]", ["r", "t"]);
polar.Jacobian(["r", "t"]).Print();   // [[cos(t), -sin(t) * r], [sin(t), cos(t) * r]]
```

A variable that no entry has gives a column of zeros, and a vector of one entry gives its
gradient as a row.

## Example: Newton's method for a system

The circle x^2 + y^2 = 4 and the hyperbola x y = 1 cross at four points. Newton's method finds
one from a nearby guess: it replaces the equations by their linearization, which the Jacobian
gives, and solves that linear system in each step. The equations and the Jacobian are derived
and compiled once:

```csharp
var equations = Vector<Expr>.Parse("[x^2 + y^2 - 4, x * y - 1]");
string[] unknowns = ["x", "y"];
equations.Jacobian(unknowns).Print();   // [[2x, 2y], [y, x]]

Func<double[], Vector<double>> f = equations.Compile(unknowns);
Func<double[], Matrix<double>> jacobian = equations.Jacobian(unknowns).Compile(unknowns);

Vector<double> guess = [2, 0];
for (int step = 0; step < 20; step++)
{
    double[] values = guess.ToArray();
    Vector<double> residual = f(values);
    if (residual.Norm() < 1e-12)
        break;
    guess -= jacobian(values).Solve(residual);
}

// After 5 steps: guess is [1.9318516525781364, 0.5176380902050416],
// that is (sqrt(2 + sqrt(3)), sqrt(2 - sqrt(3))) to the last digit or two.
```

`Solve` throws where the Jacobian is singular, such as at the guess (0, 0); a guess near the
point you want avoids that.

## Symbolic transforms

Rotations and translations with symbolic angles and offsets build chains of joints, with the
conventions of the [fixed-size matrices](fixed-size.md#fixed-size-matrices-and-transforms):
column vectors, right-handed, radians.

| Member | Result |
|---|---|
| `Matrix<Expr>.Rotation(angle)` | The 2 x 2 rotation of the plane, counterclockwise. |
| `Matrix<Expr>.RotationX(angle)`, `RotationY`, `RotationZ` | The 3 x 3 rotations about the axes. |
| `Matrix<Expr>.Translation(offset)` | The translation in homogeneous coordinates, one size larger than the offset. |
| `ToHomogeneous()` | `[[A, 0], [0, 1]]`: a linear map that combines with translations. |
| `TransformPoint(point)` | The point extended by 1, multiplied, and divided by the last entry unless that is 1. |
| `TransformDirection(direction)` | Only the linear part acts, so translations don't move it. |

```csharp
var quarterTurn = Matrix<Expr>.Rotation(ExprParser.Parse("pi / 2"));
quarterTurn.Print();   // [[0, -1], [1, 0]]
var move = Matrix<Expr>.Translation(Vector<Expr>.Parse("[a, b]"));
move.Print();          // [[1, 0, a], [0, 1, b], [0, 0, 1]]

var turnThenMove = move * quarterTurn.ToHomogeneous();
turnThenMove.TransformPoint(Vector<Expr>.Parse("[x, y]")).Print();       // [-y + a, b + x]
turnThenMove.TransformDirection(Vector<Expr>.Parse("[x, y]")).Print();   // [-y, x]
```

The entries are simplified, so known angles give exact values. A scaling is
`Matrix<Expr>.FromDiagonal(factors).ToHomogeneous()`:

```csharp
Matrix<Expr>.FromDiagonal(Vector<Expr>.Parse("[s, s]")).ToHomogeneous().Print();
// [[s, 0, 0], [0, s, 0], [0, 0, 1]]
```

## C# source

`ToCSharp(name, variables)` writes a matrix or vector as the source of a C# method, for a
project that should not depend on Epsilon: one on an older .NET or in a game engine's
scripting runtime, or an app compiled ahead of time (NativeAOT), where delegates are
interpreted rather than compiled:

```csharp
Matrix<Expr>.Rotation(new Variable("t")).ToCSharp("Rotation", ["t"]);
```

```csharp
public static void Rotation(double t, Span<double> result)
{
    double t0 = Math.Cos(t);
    result[0] = t0;
    double t1 = Math.Sin(t);
    result[1] = -t1;
    result[2] = t1;
    result[3] = t0;
}
```

The method writes entry (i, j) to `result[i * Columns + j]`, keeps a subexpression that
occurs more than once in a local, and returns exactly the values of `Evaluate`: its
parentheses keep the order of the floating-point operations. It needs `using System;` and
C# 8 or later. Generate it once, save it, and generate it again when the formula changes:

```csharp
File.WriteAllText("Rotation.cs", Matrix<Expr>.Rotation(new Variable("t")).ToCSharp("Rotation", ["t"]));
```

A name that is a C# keyword is written with an `@`; a name that is not a valid C# name throws
`ArgumentException`, and so does a variable that isn't listed.

## Example: inverse kinematics

A planar arm of three links, 3, 2 and 1 long, should reach the point (4, 2). The position of
its tip is a product of rotations and translations, and its Jacobian says how each joint moves
the tip. Both are derived and compiled once; then a few steps of damped least squares find
the angles of the joints:

```csharp
// Each link turns by the angle of its joint, then reaches along its length.
Matrix<Expr> Link(string angle, int length) =>
    Matrix<Expr>.Rotation(new Variable(angle)).ToHomogeneous() * Matrix<Expr>.Translation([length, 0]);

Vector<Expr> tip = (Link("a", 3) * Link("b", 2) * Link("c", 1)).TransformPoint([0, 0]);
string[] joints = ["a", "b", "c"];
Func<double[], Vector<double>> position = tip.Compile(joints);
Func<double[], Matrix<double>> jacobian = tip.Jacobian(joints).Compile(joints);

Vector<double> angles = [0.3, 0.3, 0.3], target = [4, 2];
for (int step = 0; step < 100; step++)
{
    double[] values = angles.ToArray();
    Vector<double> error = target - position(values);
    if (error.Norm() < 1e-9)
        break;

    // J^T (J J^T + 0.01 I)^-1 error, at most half a radian at a time.
    Matrix<double> j = jacobian(values);
    Vector<double> change = j.Transpose() * (j * j.Transpose() + 0.01 * Matrix<double>.Identity(2)).Solve(error);
    angles += change.Norm() <= 0.5 ? change : change * (0.5 / change.Norm());
}

// After 6 steps: angles about [-0.1828, 1.0812, 0.8694], the tip within 1e-10 of (4, 2).
```

The arm has three joints but the target only two coordinates, so the Jacobian is 2 x 3 and has
no inverse, unlike the one of Newton's method above. The damping keeps the steps finite where
the arm is stretched out and the Jacobian nearly singular, and the limit of half a radian
keeps the joints from spinning around. Another arm changes only the first lines: more links,
or a 3D arm with `RotationX`, `RotationY`, `RotationZ` and translations by three entries. For
an engine without Epsilon, `ToCSharp` writes `position` and `jacobian` as two methods of 15
and 29 lines.
