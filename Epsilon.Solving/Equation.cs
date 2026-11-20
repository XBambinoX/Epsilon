using Epsilon.Core;

namespace Epsilon.Solving;

/// <summary>An equation, left = right, to solve for one of its variables.</summary>
public sealed class Equation
{
    /// <summary>The equation <paramref name="left"/> = <paramref name="right"/>.</summary>
    public Equation(Expr left, Expr right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        Left = left;
        Right = right;
    }

    /// <summary>The left side.</summary>
    public Expr Left { get; }

    /// <summary>The right side.</summary>
    public Expr Right { get; }

    /// <summary>
    /// Reads an equation, two expressions in the syntax of <see cref="ExprParser.Parse"/> with
    /// <c>=</c> between them: <c>sin(x) = 1/2</c>.
    /// </summary> 
    /// <param name="input">The equation.</param>
    /// <param name="variableNames">The names of the variables, as for <see cref="ExprParser.Parse"/>.</param>
    /// <exception cref="FormatException">
    /// The input has no <c>=</c> or more than one, or a side is malformed. Positions in the
    /// message count from the start of the whole input.
    /// </exception>
    public static Equation Parse(string input, params string[] variableNames)
    {
        ArgumentNullException.ThrowIfNull(input);
        int equals = input.IndexOf('=');
        if (equals < 0)
            throw new FormatException("An equation needs '=' between its two sides.");
        int another = input.IndexOf('=', equals + 1);
        if (another >= 0)
            throw new FormatException($"An equation has one '=', but there is another at position {another}.");

        // Spaces in place of the left side keep the positions the parser reports for the right one.
        Expr left = ExprParser.Parse(input[..equals], variableNames);
        Expr right = ExprParser.Parse(new string(' ', equals + 1) + input[(equals + 1)..], variableNames);
        return new Equation(left, right);
    }

    /// <summary>Solves the equation for its only variable; see <see cref="Solve(string, Assumptions?)"/>.</summary>
    /// <exception cref="InvalidOperationException">The equation has more than one variable.</exception>
    public SolutionSet Solve() => Solve(SingleVariable(Left, Right));

    /// <summary>
    /// Solves the equation for <paramref name="variable"/> over the real numbers, exactly. The
    /// other variables are parameters, real numbers that the solutions may contain. A periodic
    /// function gives families of solutions, one value a period: sin(x) = 1/2 at
    /// π / 6 + 2 * π * k and 5 * π / 6 + 2 * π * k for every integer k (see <see cref="Solution.Period"/>).
    /// </summary>
    /// <param name="variable">The unknown.</param>
    /// <param name="assumptions">
    /// What is known about the variables. An assumption about the sign of the unknown leaves out
    /// the solutions that don't meet it.
    /// </param>
    public SolutionSet Solve(string variable, Assumptions? assumptions = null)
    {
        ArgumentNullException.ThrowIfNull(variable);
        return new Solver(variable, assumptions ?? Assumptions.None, complex: false).Solve(Left, Right);
    }

    /// <summary>Solves the equation over the complex numbers for its only variable; see <see cref="SolveComplex(string)"/>.</summary>
    /// <exception cref="InvalidOperationException">The equation has more than one variable.</exception>
    public SolutionSet SolveComplex() => SolveComplex(SingleVariable(Left, Right));

    /// <summary>
    /// Solves the equation for <paramref name="variable"/> over the complex numbers, exactly:
    /// x^2 + 1 = 0 gives i and -i. Polynomial and rational equations are solved; other parts
    /// that depend on the unknown are left <see cref="SolutionSet.Unsolved"/>.
    /// </summary>
    /// <param name="variable">The unknown.</param>
    public SolutionSet SolveComplex(string variable)
    {
        ArgumentNullException.ThrowIfNull(variable);
        return new Solver(variable, Assumptions.None, complex: true).Solve(Left, Right);
    }

    /// <summary>Solves the equation numerically for its only variable; see <see cref="SolveNumerically(string, Assumptions?)"/>.</summary>
    /// <exception cref="InvalidOperationException">The equation has more than one variable.</exception>
    public NumericSolutionSet<double> SolveNumerically() => SolveNumerically(SingleVariable(Left, Right));

    /// <summary>
    /// The real solutions for <paramref name="variable"/> as numbers: those <see cref="Solve(string, Assumptions?)"/>
    /// finds, evaluated, and every real root of a polynomial it can't solve exactly, as x^5 - x - 1.
    /// A family of solutions, as sin(x) = 1/2 has, is too many to list and stays
    /// <see cref="NumericSolutionSet{T}.Unsolved"/>; the overload with a range lists those in it.
    /// </summary>
    /// <param name="variable">The unknown.</param>
    /// <param name="assumptions">
    /// What is known about the unknown: an assumption about its sign leaves out the solutions that
    /// don't meet it.
    /// </param>
    /// <exception cref="InvalidOperationException">The equation has a variable other than <paramref name="variable"/>.</exception>
    public NumericSolutionSet<double> SolveNumerically(string variable, Assumptions? assumptions = null)
    {
        ArgumentNullException.ThrowIfNull(variable);
        RequireOnly(variable);
        var solver = new Solver(variable, assumptions ?? Assumptions.None, complex: false, numeric: true);
        return NumericSolutions.Real(solver.Solve(Left, Right), solver);
    }

    /// <summary>Solves the equation numerically in a range for its only variable; see <see cref="SolveNumerically(string, double, double, Assumptions?)"/>.</summary>
    /// <exception cref="InvalidOperationException">The equation has more than one variable.</exception>
    public NumericSolutionSet<double> SolveNumerically(double from, double to) => SolveNumerically(SingleVariable(Left, Right), from, to);

    /// <summary>
    /// The real solutions for <paramref name="variable"/> in [<paramref name="from"/>, <paramref name="to"/>]
    /// as numbers: those of <see cref="SolveNumerically(string, Assumptions?)"/> in the range, and
    /// those of the parts it doesn't solve, as sin(x) or x - cos(x), found by scanning the range.
    /// Points where the equation is undefined are never solutions.
    /// </summary>
    /// <remarks>
    /// The scan steps through the range on a grid, finer where the equation oscillates through
    /// sines and cosines; solutions closer together than a step can be missed. An infinite limit
    /// is scanned through a change of variable, with steps that get longer far from 0.
    /// </remarks>
    /// <param name="variable">The unknown.</param>
    /// <param name="from">The lower end of the range; may be -infinity.</param>
    /// <param name="to">The upper end of the range; may be infinity.</param>
    /// <param name="assumptions">What is known about the unknown, as for <see cref="SolveNumerically(string, Assumptions?)"/>.</param>
    /// <exception cref="ArgumentException">A limit is NaN, or <paramref name="from"/> isn't less than <paramref name="to"/>.</exception>
    /// <exception cref="InvalidOperationException">The equation has a variable other than <paramref name="variable"/>.</exception>
    public NumericSolutionSet<double> SolveNumerically(string variable, double from, double to, Assumptions? assumptions = null)
    {
        ArgumentNullException.ThrowIfNull(variable);
        RequireRange(from, to);
        RequireOnly(variable);
        var solver = new Solver(variable, assumptions ?? Assumptions.None, complex: false, numeric: true, region: (from, to));
        return NumericSolutions.Real(solver.Solve(Left, Right), solver);
    }

    /// <summary>Looks for a solution near a guess for the only variable; see <see cref="FindRoot(string, double)"/>.</summary>
    /// <exception cref="InvalidOperationException">The equation has more than one variable.</exception>
    public double? FindRoot(double near) => FindRoot(SingleVariable(Left, Right), near);

    /// <summary>
    /// A real solution for <paramref name="variable"/> by Newton's method from <paramref name="near"/>,
    /// to about full double precision. Which solution it finds depends on the guess.
    /// </summary>
    /// <param name="variable">The unknown.</param>
    /// <param name="near">Where to start.</param>
    /// <returns>The solution, or null if the iteration doesn't converge (a flat slope, divergence, too many steps).</returns>
    /// <exception cref="InvalidOperationException">The equation has a variable other than <paramref name="variable"/>.</exception>
    public double? FindRoot(string variable, double near)
    {
        ArgumentNullException.ThrowIfNull(variable);
        RequireOnly(variable);
        return RealRoots.Near((Left - Right).Simplify(SimplifyMode.Strict), variable, near);
    }

    /// <summary>Solves the equation numerically over the complex numbers for its only variable; see <see cref="SolveComplexNumerically(string)"/>.</summary>
    /// <exception cref="InvalidOperationException">The equation has more than one variable.</exception>
    public NumericSolutionSet<ComplexNumber> SolveComplexNumerically() => SolveComplexNumerically(SingleVariable(Left, Right));

    /// <summary>
    /// The complex solutions for <paramref name="variable"/> as numbers: those <see cref="SolveComplex(string)"/>
    /// finds, evaluated, and every root of a polynomial it can't solve exactly.
    /// </summary>
    /// <param name="variable">The unknown.</param>
    /// <exception cref="InvalidOperationException">The equation has a variable other than <paramref name="variable"/>.</exception>
    public NumericSolutionSet<ComplexNumber> SolveComplexNumerically(string variable)
    {
        ArgumentNullException.ThrowIfNull(variable);
        RequireOnly(variable);
        var solver = new Solver(variable, Assumptions.None, complex: true, numeric: true);
        return NumericSolutions.Complex(solver.Solve(Left, Right), solver);
    }

    /// <summary>Solves the equation numerically in a rectangle for its only variable; see <see cref="SolveComplexNumerically(string, ComplexNumber, ComplexNumber)"/>.</summary>
    /// <exception cref="InvalidOperationException">The equation has more than one variable.</exception>
    public NumericSolutionSet<ComplexNumber> SolveComplexNumerically(ComplexNumber from, ComplexNumber to) =>
        SolveComplexNumerically(SingleVariable(Left, Right), from, to);

    /// <summary>
    /// The complex solutions for <paramref name="variable"/> in the rectangle with the corners
    /// <paramref name="from"/> (lower left) and <paramref name="to"/> (upper right), as numbers:
    /// those of <see cref="SolveComplexNumerically(string)"/> in it, and those of the parts it
    /// doesn't solve, as exp(x) - 1, found by Newton's method from every point of a grid.
    /// Points where the equation is undefined are never solutions.
    /// </summary>
    /// <remarks>
    /// Without a completeness guarantee: a solution is found only if some start of the grid
    /// converges to it. The grid is finer where the equation oscillates through sines and cosines.
    /// </remarks>
    /// <param name="variable">The unknown.</param>
    /// <param name="from">The lower left corner.</param>
    /// <param name="to">The upper right corner.</param>
    /// <exception cref="ArgumentException">A corner isn't finite, or <paramref name="from"/> isn't below and left of <paramref name="to"/>.</exception>
    /// <exception cref="InvalidOperationException">The equation has a variable other than <paramref name="variable"/>.</exception>
    public NumericSolutionSet<ComplexNumber> SolveComplexNumerically(string variable, ComplexNumber from, ComplexNumber to)
    {
        ArgumentNullException.ThrowIfNull(variable);
        if (!double.IsFinite(from.Magnitude) || !double.IsFinite(to.Magnitude))
            throw new ArgumentException("A corner of the rectangle isn't a finite number.");
        if (!(from.Real < to.Real && from.Imaginary < to.Imaginary))
            throw new ArgumentException($"Expected the lower left corner first, got {from} and {to}.");
        RequireOnly(variable);
        var solver = new Solver(variable, Assumptions.None, complex: true, numeric: true, region: (from, to));
        return NumericSolutions.Complex(solver.Solve(Left, Right), solver);
    }

    /// <summary>Looks for a complex solution near a guess for the only variable; see <see cref="FindComplexRoot(string, ComplexNumber)"/>.</summary>
    /// <exception cref="InvalidOperationException">The equation has more than one variable.</exception>
    public ComplexNumber? FindComplexRoot(ComplexNumber near) => FindComplexRoot(SingleVariable(Left, Right), near);

    /// <summary>
    /// A complex solution for <paramref name="variable"/> by Newton's method from <paramref name="near"/>,
    /// to about full double precision. Which solution it finds depends on the guess.
    /// </summary>
    /// <param name="variable">The unknown.</param>
    /// <param name="near">Where to start.</param>
    /// <returns>The solution, or null if the iteration doesn't converge.</returns>
    /// <exception cref="InvalidOperationException">The equation has a variable other than <paramref name="variable"/>.</exception>
    public ComplexNumber? FindComplexRoot(string variable, ComplexNumber near)
    {
        ArgumentNullException.ThrowIfNull(variable);
        RequireOnly(variable);
        return ComplexRoots.Near((Left - Right).Simplify(SimplifyMode.Strict), variable, near);
    }

    /// <summary>The equation as text: <c>sin(x) = 1/2</c>.</summary>
    public string Print() => $"{Left.Print()} = {Right.Print()}";

    /// <summary>The equation in LaTeX.</summary>
    public string ToLatex() => $"{Left.ToLatex()} = {Right.ToLatex()}";

    /// <inheritdoc/>
    public override string ToString() => Print();

    private static void RequireRange(double from, double to)
    {
        if (double.IsNaN(from) || double.IsNaN(to))
            throw new ArgumentException("A limit of the range is NaN.");
        if (!(from < to))
            throw new ArgumentException(FormattableString.Invariant($"Expected the lower limit below the upper one, got [{from}, {to}]."));
    }

    private void RequireOnly(string variable)
    {
        var others = new SortedSet<string>(Left.GetVariables(), StringComparer.Ordinal);
        others.UnionWith(Right.GetVariables());
        others.Remove(variable);
        if (others.Count > 0)
            throw new InvalidOperationException(
                $"Expected no variable but {variable}, found [{string.Join(", ", others)}]. Substitute numbers for the others first.");
    }

    private static string SingleVariable(Expr left, Expr right)
    {
        var variables = new SortedSet<string>(left.GetVariables(), StringComparer.Ordinal);
        variables.UnionWith(right.GetVariables());
        return variables.Count switch
        {
            0 => "x",
            1 => variables.Min!,
            _ => throw new InvalidOperationException(
                $"Expected exactly 1 variable, found {variables.Count}: [{string.Join(", ", variables)}]. " +
                "Name the unknown with the overload that takes it."),
        };
    }
}
