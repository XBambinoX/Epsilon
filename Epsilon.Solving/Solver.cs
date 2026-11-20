using Epsilon.Core;

namespace Epsilon.Solving;

// Numeric, it finds the roots of polynomials it can't solve exactly as numbers, and skips the
// radicals, which numbers give more accurately. With a region too, an interval of the real line
// or a rectangle of the complex plane between two corners, it keeps the solutions in it and
// searches it for those of the rest.
// A solver for an equation inside another shares its numbers.
internal sealed partial class Solver(
    string variable, Assumptions assumptions, bool complex, bool numeric = false, (ComplexNumber Low, ComplexNumber High)? region = null, Solver? outer = null)
{
    private const int MaxDegree = 64;

    // In numeric mode, the quadratic with two roots each root came from: as a double, centre -
    // half loses digits where the two nearly cancel, and Newton's method on it gets them back.
    private readonly Dictionary<Expr, ComplexNumber[]> _quadratics = outer?._quadratics ?? [];

    // The roots found numerically, in full: as a Constant, a double keeps 15 digits.
    private readonly Dictionary<Expr, ComplexNumber> _numbers = outer?._numbers ?? [];

    internal SolutionSet Solve(Expr left, Expr right)
    {
        // Strict, so that no step enlarges the domain: x/x = 1 isn't 1 = 1 at x = 0.
        left = left.Simplify(assumptions, SimplifyMode.Strict);
        right = right.Simplify(assumptions, SimplifyMode.Strict);
        Expr difference = (left - right).Simplify(assumptions, SimplifyMode.Strict);

        if (!difference.DependsOn(variable))
            return WithoutUnknown(difference, left, right);

        List<Condition> conditions = [.. Domain.Conditions(difference, variable, complex), .. SignAssumed()];
        Expr numerator = Together(difference).Numerator.Simplify(assumptions, SimplifyMode.Strict);
        if (!numerator.DependsOn(variable))
            return EveryValueWhere(numerator, conditions);

        var candidates = new List<Expr>();
        var isolated = new List<(Solution Solution, Expr Factor)>();
        var unsolved = new List<Expr>();
        foreach (Expr factor in Factors(numerator).Where(factor => factor.DependsOn(variable)))
        {
            if (!complex && Polynomials.Coefficients(factor, variable, MaxDegree) is null && Isolated(factor) is { } found)
                isolated.AddRange(found.Select(solution => (solution, factor)));
            else
                SolveFactor(factor, candidates, unsolved);
        }

        // Where the factor is defined holds by how its solutions were found.
        List<Solution> solutions = [
            .. candidates.SelectMany(candidate => Admitted(candidate, [], conditions)),
            .. isolated.SelectMany(item => Admitted(item.Solution.Value, item.Solution.Conditions,
                [.. conditions.Except(Domain.Conditions(item.Factor, variable, complex))])),
        ];
        if (region is var (low, high))
        {
            solutions.RemoveAll(solution => !Within(Number(solution.Value), low, high));
            if (unsolved.Count > 0)
            {
                solutions.AddRange(Searched(difference, low, high, [.. solutions.Select(solution => Number(solution.Value))]));
                unsolved.Clear();
            }
        }

        return SolutionSet.Of(variable, solutions, unsolved, complex);
    }

    private bool Within(ComplexNumber z, ComplexNumber low, ComplexNumber high) =>
        low.Real <= z.Real && z.Real <= high.Real && (!complex || low.Imaginary <= z.Imaginary && z.Imaginary <= high.Imaginary);

    // Where every real value is a solution, the interval it is in.
    private IEnumerable<Condition> InRange()
    {
        if (complex || region is not var (low, high))
            yield break;
        if (double.IsFinite(low.Real))
            yield return new Condition(new Variable(variable), Comparison.GreaterOrEqual, low.Real);
        if (double.IsFinite(high.Real))
            yield return new Condition(new Variable(variable), Comparison.LessOrEqual, high.Real);
    }

    // The roots of the whole difference in the region, besides those already found: evaluating it
    // keeps out the points where the equation is undefined, the edges of its domain included.
    private IEnumerable<Solution> Searched(Expr difference, ComplexNumber low, ComplexNumber high, List<ComplexNumber> found)
    {
        IEnumerable<ComplexNumber> roots = complex
            ? ComplexRoots.InRectangle(difference, variable, low, high)
            : RealRoots.InRange(difference, variable, low.Real, high.Real).Select(root => new ComplexNumber(root));
        List<Condition> signs = [.. SignAssumed()];
        foreach (ComplexNumber root in roots)
        {
            if (found.Any(number => (number - root).Magnitude <= 1e-6 * Math.Max(1, root.Magnitude)))
                continue;

            Expr value = root.Imaginary == 0 ? root.Real : (new Constant(root.Real) + new Constant(root.Imaginary) * Expr.I).Simplify();
            if (signs.Any(sign => Decide(new Condition(value, sign.Comparison, sign.Right)) == false))
                continue;

            _numbers[value] = root;
            yield return new Solution(value, []);
        }
    }

    private SolutionSet WithoutUnknown(Expr difference, Expr left, Expr right)
    {
        if (difference is Constant { Value.IsZero: true })
            return SolutionSet.EveryValue(variable, [.. InRange()], complex);
        if (IsNonZero(difference))
            return SolutionSet.Empty(variable, complex);
        return SolutionSet.EveryValue(variable, [new Condition(left, Comparison.Equal, right), .. InRange()], complex);
    }

    // The numerator doesn't depend on the unknown: if it is 0, every value where the equation is
    // defined is a solution.
    private SolutionSet EveryValueWhere(Expr numerator, List<Condition> conditions)
    {
        if (numerator is not Constant { Value.IsZero: true })
        {
            if (IsNonZero(numerator))
                return SolutionSet.Empty(variable, complex);
            conditions.Insert(0, new Condition(numerator, Comparison.Equal, 0));
        }

        var open = new List<Condition>();
        foreach (Condition condition in conditions)
        {
            switch (Decide(condition))
            {
                case false:
                    return SolutionSet.Empty(variable, complex);
                case null:
                    open.Add(Tidied(condition));
                    break;
            }
        }

        return SolutionSet.EveryValue(variable, [.. open, .. InRange()], complex);
    }

    // The number in a condition on the right and no minus in front on the left: a != 1 for
    // a - 1 != 0, a < 0 for -a > 0.
    private Condition Tidied(Condition condition)
    {
        Expr left = (condition.Left - condition.Right).Simplify(assumptions);
        Expr right = 0;
        if (left is Add { Right: Constant term } sum && sum.Left.GetVariables().Count > 0)
            (left, right) = (sum.Left, new Constant(-term.Value));
        else if (left is Subtract { Right: Constant subtracted } difference && difference.Left.GetVariables().Count > 0)
            (left, right) = (difference.Left, subtracted);

        if (!left.Print().StartsWith('-'))
            return new Condition(left, condition.Comparison, right);

        Comparison flipped = condition.Comparison switch
        {
            Comparison.Less => Comparison.Greater,
            Comparison.LessOrEqual => Comparison.GreaterOrEqual,
            Comparison.Greater => Comparison.Less,
            Comparison.GreaterOrEqual => Comparison.LessOrEqual,
            var same => same,
        };
        return new Condition((-left).Simplify(assumptions), flipped, (-right).Simplify(assumptions));
    }

    private void SolveFactor(Expr factor, List<Expr> candidates, List<Expr> unsolved)
    {
        if (Polynomials.Coefficients(factor, variable, MaxDegree) is not { Length: >= 2 } coefficients)
        {
            unsolved.Add(factor);
            return;
        }

        switch (coefficients.Length - 1)
        {
            case 1 when IsNonZero(coefficients[1]):
                candidates.Add((-coefficients[0] / coefficients[1]).Simplify(assumptions));
                return;
            case 2 when Quadratic(coefficients[0], coefficients[1], coefficients[2]) is { } roots:
                candidates.AddRange(roots);
                if (numeric && roots.Count == 2)
                    foreach (Expr root in roots)
                        _quadratics[root] = [.. coefficients.Select(coefficient => coefficient.EvaluateComplex())];
                return;
            case > 2 when Split(factor, coefficients.Length - 1) is { } parts:
                foreach (Expr part in parts)
                    SolveFactor(part, candidates, unsolved);
                return;
        }

        if (coefficients.Length > 3 && Polynomials.Rationals(coefficients) is { } rationals)
        {
            Rational[] squareFree = Polynomials.SquareFree(rationals);
            if (squareFree.Length < rationals.Length)
            {
                SolveFactor(Polynomials.Of(squareFree.Select(coefficient => (Expr)coefficient), variable), candidates, unsolved);
                return;
            }

            if (!numeric && InRadicals(rationals) is { } roots)
            {
                candidates.AddRange(roots);
                return;
            }
        }

        if (numeric && Approximately(coefficients) is { } approximations)
            candidates.AddRange(approximations);
        else
            unsolved.Add(factor);
    }

    // A solution found in numeric mode, as a number to about full double precision.
    internal ComplexNumber Number(Expr value) =>
        _numbers.TryGetValue(value, out ComplexNumber number) ? number
        : _quadratics.TryGetValue(value, out ComplexNumber[]? quadratic) ? NumericRoots.Polished(quadratic, value.EvaluateComplex())
        : value.EvaluateComplex();

    // The roots of a cubic or a quartic in radicals.
    private List<Expr>? InRadicals(Rational[] p)
    {
        Rational[] monic = [.. p.Select(coefficient => coefficient / p[^1])];
        if (monic.Length == 4)
            return Radicals.Cubic(monic, complex);
        if (monic.Length != 5 || Radicals.Quartic(monic, variable) is not { } quadratics)
            return null;

        var roots = new List<Expr>();
        foreach (var (b, c) in quadratics)
        {
            if (Quadratic(c, b, 1) is not { } pair)
                return null;
            roots.AddRange(pair);
        }

        return roots;
    }

    // The roots of a polynomial with numbers for coefficients, found numerically: the real ones,
    // or all of them over the complex numbers.
    private List<Expr>? Approximately(Expr[] coefficients)
    {
        if (coefficients.Any(coefficient => coefficient.GetVariables().Count > 0))
            return null;
        ComplexNumber[] p = [.. coefficients.Select(coefficient => coefficient.EvaluateComplex())];
        if (!p.All(coefficient => double.IsFinite(coefficient.Magnitude)) || NumericRoots.Of(p) is not { } roots)
            return null;

        var found = new List<Expr>();
        foreach (ComplexNumber root in roots)
        {
            // A part that rounding alone could give is 0.
            double tiny = 1e-10 * Math.Max(1, root.Magnitude);
            double real = Math.Abs(root.Real) <= tiny ? 0 : root.Real, imaginary = Math.Abs(root.Imaginary) <= tiny ? 0 : root.Imaginary;
            if (imaginary != 0 && !complex)
                continue;

            Expr value = imaginary == 0 ? real : (new Constant(real) + new Constant(imaginary) * Expr.I).Simplify();
            _numbers[value] = new ComplexNumber(real, imaginary);
            found.Add(value);
        }

        return found;
    }

    // a x^2 + b x + c = 0 by the discriminant, or null where its sign isn't known.
    private List<Expr>? Quadratic(Expr c, Expr b, Expr a)
    {
        if (!IsNonZero(a))
            return null;

        // The centre and the distance to each root apart, so that their numbers cancel:
        // -1 + sqrt(2), not (-2 + 2sqrt(2)) / 2.
        Expr discriminant = (b * b - 4 * a * c).Simplify(assumptions);
        Expr centre = (-b / (2 * a)).Simplify(assumptions);
        Expr Half(Expr d) => (new Sqrt(d) / (2 * a)).Simplify(assumptions);
        return SignOf(discriminant) switch
        {
            > 0 => [(centre - Half(discriminant)).Simplify(assumptions), (centre + Half(discriminant)).Simplify(assumptions)],
            0 => [centre],
            < 0 when complex => [(centre - Expr.I * Half(-discriminant)).Simplify(assumptions), (centre + Expr.I * Half(-discriminant)).Simplify(assumptions)],
            < 0 => [],
            null => null,
        };
    }

    // The factors of a polynomial of higher degree with rational coefficients, each of a lower
    // degree: its rational roots and the quadratics TryFactorReal finds.
    private List<Expr>? Split(Expr polynomial, int degree)
    {
        var (factored, success) = polynomial.TryFactorReal(variable);
        if (!success)
            return null;

        List<Expr> parts = [.. Factors(factored).Where(part => part.DependsOn(variable))];
        return parts.All(part => Polynomials.Coefficients(part, variable, MaxDegree)?.Length - 1 < degree) ? parts : null;
    }

    // The value as a solution, with the conditions on it that aren't decided; nothing where one
    // fails.
    private IEnumerable<Solution> Admitted(Expr value, IReadOnlyList<Condition> given, List<Condition> conditions)
    {
        var open = new List<Condition>(given);
        foreach (Condition condition in conditions)
        {
            var at = new Condition(
                condition.Left.Substitute(variable, value).Simplify(assumptions),
                condition.Comparison,
                condition.Right.Substitute(variable, value).Simplify(assumptions));
            switch (Decide(at))
            {
                case false:
                    yield break;
                case null:
                    open.Add(Tidied(at));
                    break;
            }
        }

        yield return new Solution(value, open);
    }

    private IEnumerable<Condition> SignAssumed()
    {
        if (complex)
            yield break;

        var x = new Variable(variable);
        if (assumptions.IsPositive(variable))
            yield return new Condition(x, Comparison.Greater, 0);
        else if (assumptions.IsNegative(variable))
            yield return new Condition(x, Comparison.Less, 0);
        else if (assumptions.IsNonNegative(variable))
            yield return new Condition(x, Comparison.GreaterOrEqual, 0);
        else if (assumptions.IsNonPositive(variable))
            yield return new Condition(x, Comparison.LessOrEqual, 0);
        else if (assumptions.IsNonZero(variable))
            yield return new Condition(x, Comparison.NotEqual, 0);
    }

    // Whether the condition holds, fails, or isn't known to do either.
    private bool? Decide(Condition condition)
    {
        Expr difference = (condition.Left - condition.Right).Simplify(assumptions);
        if (SignOf(difference) is int sign)
        {
            return condition.Comparison switch
            {
                Comparison.Equal => sign == 0,
                Comparison.NotEqual => sign != 0,
                Comparison.Less => sign < 0,
                Comparison.LessOrEqual => sign <= 0,
                Comparison.Greater => sign > 0,
                _ => sign >= 0,
            };
        }

        if (complex)
            return condition.Comparison switch
            {
                Comparison.Equal when IsNonZero(difference) => false,
                Comparison.NotEqual when IsNonZero(difference) => true,
                _ => null,
            };

        return condition.Comparison switch
        {
            Comparison.Equal when difference.IsProvablyNonZero(assumptions) => false,
            Comparison.NotEqual when difference.IsProvablyNonZero(assumptions) => true,
            Comparison.Less when difference.IsProvablyNegative(assumptions) => true,
            Comparison.Less when difference.IsProvablyNonNegative(assumptions) => false,
            Comparison.LessOrEqual when difference.IsProvablyNonPositive(assumptions) => true,
            Comparison.LessOrEqual when difference.IsProvablyPositive(assumptions) => false,
            Comparison.Greater when difference.IsProvablyPositive(assumptions) => true,
            Comparison.Greater when difference.IsProvablyNonPositive(assumptions) => false,
            Comparison.GreaterOrEqual when difference.IsProvablyNonNegative(assumptions) => true,
            Comparison.GreaterOrEqual when difference.IsProvablyNegative(assumptions) => false,
            _ => null,
        };
    }

    // The sign of a real number, by its value where the assumptions alone don't decide it (pi - 3);
    // null for one that isn't known or isn't real.
    private int? SignOf(Expr expr)
    {
        if (expr is Constant constant)
            return constant.Value.Sign;
        if (new Sign(expr).Simplify(assumptions) is Constant sign)
            return sign.Value.Sign;
        return expr.IsProvablyPositive(assumptions) ? 1 : expr.IsProvablyNegative(assumptions) ? -1 : null;
    }

    private bool IsNonZero(Expr expr)
    {
        if (SignOf(expr) is int sign)
            return sign != 0;
        if (expr.IsProvablyNonZero(assumptions))
            return true;

        // A complex number by its value, as 1 + i, which has no sign.
        if (!complex || expr.GetVariables().Count > 0)
            return false;
        ComplexNumber value = expr.EvaluateComplex();
        return double.IsFinite(value.Magnitude) && value.Magnitude > 1e-9;
    }

    internal static IEnumerable<Expr> Factors(Expr expr) => expr switch
    {
        Multiply product => Factors(product.Left).Concat(Factors(product.Right)),
        Negate negation => Factors(negation.Argument),
        Power { Exponent: Constant { Value: { IsInteger: true } n } } power when n.Sign > 0 => Factors(power.Base),
        _ => [expr],
    };

    // expr as a numerator over a denominator, with the fractions in it brought together.
    private static (Expr Numerator, Expr Denominator) Together(Expr expr)
    {
        switch (expr)
        {
            case Add sum:
                return Sum(Together(sum.Left), Together(sum.Right), subtract: false);
            case Subtract difference:
                return Sum(Together(difference.Left), Together(difference.Right), subtract: true);
            case Negate negation:
                var (n, d) = Together(negation.Argument);
                return (-n, d);
            case Multiply product:
                var (leftNumerator, leftDenominator) = Together(product.Left);
                var (rightNumerator, rightDenominator) = Together(product.Right);
                return (leftNumerator * rightNumerator, Times(leftDenominator, rightDenominator));
            case Divide quotient:
                var (top, topDenominator) = Together(quotient.Numerator);
                var (bottom, bottomDenominator) = Together(quotient.Denominator);
                return (top * bottomDenominator, Times(topDenominator, bottom));
            case Power { Exponent: Constant { Value: { IsInteger: true } k } } power when k.Sign < 0:
                var (baseNumerator, baseDenominator) = Together(power.Base);
                return (baseDenominator.Pow(-k), baseNumerator.Pow(-k));
            default:
                return (expr, 1);
        }

        static (Expr, Expr) Sum((Expr N, Expr D) left, (Expr N, Expr D) right, bool subtract)
        {
            if (left.D.Equals(right.D))
                return (subtract ? left.N - right.N : left.N + right.N, left.D);
            Expr leftPart = left.N * right.D, rightPart = right.N * left.D;
            return (subtract ? leftPart - rightPart : leftPart + rightPart, Times(left.D, right.D));
        }

        static Expr Times(Expr a, Expr b) =>
            a is Constant { Value.IsOne: true } ? b : b is Constant { Value.IsOne: true } ? a : a * b;
    }
}
