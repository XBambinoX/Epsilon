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
        if (outer is null)
            _index = FreeIndex(variable, left, right);

        // Strict, so that no step enlarges the domain: x/x = 1 isn't 1 = 1 at x = 0.
        left = left.Simplify(assumptions, SimplifyMode.Strict);
        right = right.Simplify(assumptions, SimplifyMode.Strict);
        Expr difference = (left - right).Simplify(assumptions, SimplifyMode.Strict);

        if (!difference.DependsOn(variable))
            return WithoutUnknown(difference, left, right);

        // The parameters' own, as a > 0 for x = ln(a), once: an equation inside another is built
        // by steps whose needs already keep the values they are undefined at out.
        List<Condition> conditions = [.. Domain.Conditions(difference, variable, complex, parameters: outer is null), .. SignAssumed()];
        Expr numerator = Together(difference).Numerator.Simplify(assumptions, SimplifyMode.Strict);
        if (!numerator.DependsOn(variable))
            return EveryValueWhere(numerator, conditions);

        var candidates = new List<Solution>();
        var isolated = new List<(Solution Solution, Expr Factor)>();
        var rationalized = new List<(Solution Solution, List<Condition> Needs)>();
        var substituted = new List<(Solution Solution, Expr Kernel)>();
        var unsolved = new List<Expr>();
        foreach (Expr factor in Factors(numerator).Where(factor => factor.DependsOn(variable)))
        {
            // A polynomial left unsolved may still be undone: x^3 = a.
            bool polynomial = Polynomials.Coefficients(factor, variable, MaxDegree) is not null;
            var rest = new List<Expr>();
            if (complex || polynomial)
                SolveFactor(factor, candidates, rest);
            else
                rest.Add(factor);

            foreach (Expr part in rest)
            {
                if (complex)
                    unsolved.Add(part);
                else if (Isolated(part) is { } found)
                    isolated.AddRange(found.Select(solution => (solution, part)));
                else if (!polynomial && Substituted(part) is var (values, kernel))
                    substituted.AddRange(values.Select(solution => (solution, kernel)));
                else if (!polynomial && Rationalized(part) is { } squared)
                    rationalized.AddRange(squared);
                else
                    unsolved.Add(part);
            }
        }

        // Where the factor is defined holds by how isolated solutions were found, and where the
        // part substituted is, by how its values were undone.
        List<Solution> solutions = Reduced([
            .. candidates.SelectMany(candidate => Admitted(candidate, conditions)),
            .. isolated.SelectMany(item => Admitted(item.Solution, [.. conditions.Except(Domain.Conditions(item.Factor, variable, complex))])),
            .. rationalized.SelectMany(item => Admitted(item.Solution, [.. conditions, .. item.Needs])),
            .. substituted.SelectMany(item => Admitted(item.Solution, [.. conditions.Except(Domain.Conditions(item.Kernel, variable, complex))])),
        ]);
        if (region is var (low, high))
        {
            // A family the range can't list, as one in an infinite range, is left to the scan.
            bool scan = unsolved.Count > 0;
            var inRegion = new List<Solution>();
            foreach (Solution solution in solutions)
            {
                if (solution.Index is null)
                {
                    if (Within(Number(solution.Value), low, high))
                        inRegion.Add(solution);
                }
                else if (Numbers(solution, low.Real, high.Real, difference) is { } values)
                    inRegion.AddRange(values);
                else
                    scan = true;
            }

            solutions = inRegion;
            if (scan)
            {
                solutions.AddRange(Searched(difference, low, high, [.. solutions.Select(solution => Number(solution.Value))]));
                unsolved.Clear();
            }
        }

        return SolutionSet.Of(variable, solutions, unsolved, complex, this);
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

        // By the real part, as a family listed in the range can have found many.
        ComplexNumber[] sorted = [.. found.OrderBy(number => number.Real)];
        double[] reals = [.. sorted.Select(number => number.Real)];
        bool Found(ComplexNumber root)
        {
            double tolerance = 1e-6 * Math.Max(1, root.Magnitude);
            int i = Array.BinarySearch(reals, root.Real - tolerance);
            for (i = i < 0 ? ~i : i; i < reals.Length && reals[i] <= root.Real + tolerance; i++)
            {
                if ((sorted[i] - root).Magnitude <= tolerance)
                    return true;
            }

            return false;
        }

        foreach (ComplexNumber root in roots)
        {
            if (Found(root))
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
            return SolutionSet.EveryValue(variable, [.. InRange()], complex, this);
        if (IsNonZero(difference))
            return SolutionSet.Empty(variable, complex, this);
        return SolutionSet.EveryValue(variable, [new Condition(left, Comparison.Equal, right), .. InRange()], complex, this);
    }

    // The numerator doesn't depend on the unknown: if it is 0, every value where the equation is
    // defined is a solution.
    private SolutionSet EveryValueWhere(Expr numerator, List<Condition> conditions)
    {
        if (numerator is not Constant { Value.IsZero: true })
        {
            if (IsNonZero(numerator))
                return SolutionSet.Empty(variable, complex, this);
            conditions.Insert(0, new Condition(numerator, Comparison.Equal, 0));
        }

        var open = new List<Condition>();
        foreach (Condition condition in conditions)
        {
            switch (Decide(condition))
            {
                case false:
                    return SolutionSet.Empty(variable, complex, this);
                case null:
                    open.Add(Tidied(condition));
                    break;
            }
        }

        return SolutionSet.EveryValue(variable, [.. open, .. InRange()], complex, this);
    }

    // The number in a condition on the right and no minus in front on the left: a != 1 for
    // a - 1 != 0, a < 0 for -a > 0; without what doesn't change it: a != 0 for a^2 / 2 != 0,
    // a > 0 for 1 / a >= 0. On the index of a family, a bound: k >= 0.
    private Condition Tidied(Condition condition)
    {
        if (OnIndex(condition) && IndexBound(condition).Bound is { } bound)
            return bound;

        Comparison comparison = condition.Comparison;
        Expr left = Shortest(condition.Left - condition.Right, comparison is Comparison.Equal or Comparison.NotEqual);
        for (Expr? peeled = left; peeled is not null; peeled = Peeled(left, ref comparison))
            left = peeled;

        bool equality = comparison is Comparison.Equal or Comparison.NotEqual;
        (Expr Left, Expr Right) Moved(Expr l, Expr r) => l switch
        {
            Add sum when sum.Right.GetVariables().Count == 0 && sum.Left.GetVariables().Count > 0 =>
                (sum.Left, (r - sum.Right).Simplify(assumptions)),
            Subtract difference when difference.Right.GetVariables().Count == 0 && difference.Left.GetVariables().Count > 0 =>
                (difference.Left, (r + difference.Right).Simplify(assumptions)),
            Subtract difference when equality && r is Constant { Value.IsZero: true } && difference.Left.GetVariables().Count > 0 =>
                (difference.Left, difference.Right),
            _ => (l, r),
        };

        Expr right = 0;
        Settle(Moved(left, 0));

        // The logarithm grows with its argument, which is positive by the domain: a != 1 for
        // ln(a) != 0, a <= e for ln(a) <= 1.
        if (left is Ln { Argument: var argument } && right.GetVariables().Count == 0)
            Settle(Moved(argument, right is Constant { Value.IsOne: true } ? Expr.E : new Exp(right).Simplify(assumptions)));

        // The root of a square is its absolute value; another root, compared with a number that
        // isn't negative, is the argument compared with the square: a <= 4 for sqrt(a) <= 2.
        if (left is Sqrt { Argument: var radicand } && right.GetVariables().Count == 0)
        {
            if (SquareRoot(radicand) is { } root)
                left = new Abs(root);
            else if (SignOf(right) >= 0)
                Settle(Moved(radicand, (right * right).Simplify(assumptions)));
        }

        // abs(a) <= 2 for a^2 <= 4; abs(a) >= 1 for abs(1 / a) <= 1, abs(a) <= 1 for abs(2a) <= 2.
        if (left is Power { Exponent: Constant { Value: var exponent } } square && exponent == 2 && right.GetVariables().Count == 0 && SignOf(right) > 0)
            (left, right) = (new Abs(square.Base), new Sqrt(right).Simplify(assumptions));
        while (left is Abs { Argument: var inside } && right.GetVariables().Count == 0 && Unscaled(inside) is { } unscaled)
            left = new Abs(unscaled);

        return new Condition(left, comparison, right);

        // No minus in front, nor a number's factor: a <= 1 for -a >= -1, 4a <= 4 and a / 2 <= 1/2.
        void Settle((Expr Left, Expr Right) moved)
        {
            (left, right) = moved;
            if (left.Print().StartsWith('-'))
                (left, right, comparison) = ((-left).Simplify(assumptions), (-right).Simplify(assumptions), Flipped(comparison));
            if (left is Multiply { Left: Constant { Value: var factor } } scaled && factor.Sign > 0 && scaled.Right.GetVariables().Count > 0)
                (left, right) = (scaled.Right, (right / factor).Simplify(assumptions));
            else if (left is Divide { Denominator: Constant { Value: var divisor } } divided && divisor.Sign > 0)
                (left, right) = (divided.Numerator, (right * divisor).Simplify(assumptions));
        }

        Expr? Unscaled(Expr inside)
        {
            Expr Size(Expr number) => new Abs(number).Simplify(assumptions);
            int? SignOfNumber(Expr number) => number.GetVariables().Count == 0 && SignOf(number) is int s and not 0 ? s : null;
            switch (inside)
            {
                case Negate negation:
                    return negation.Argument;
                case Multiply product when SignOfNumber(product.Left) is not null:
                    right = (right / Size(product.Left)).Simplify(assumptions);
                    return product.Right;
                case Multiply product when SignOfNumber(product.Right) is not null:
                    right = (right / Size(product.Right)).Simplify(assumptions);
                    return product.Left;
                case Divide quotient when SignOfNumber(quotient.Denominator) is not null:
                    right = (right * Size(quotient.Denominator)).Simplify(assumptions);
                    return quotient.Numerator;
                case Divide quotient when SignOfNumber(quotient.Numerator) is not null && SignOf(right) > 0 && comparison is not (Comparison.Equal or Comparison.NotEqual):
                    (right, comparison) = ((Size(quotient.Numerator) / right).Simplify(assumptions), Flipped(comparison));
                    return quotient.Denominator;
                default:
                    return null;
            }
        }
    }

    // The shortest of e simplified, expanded and, where only whether it is 0 matters, its
    // numerator over a common denominator: 4a + 1 for -4a^2 + (2a + 1)^2, 1 for (a b + 1) / b - a.
    private Expr Shortest(Expr e, bool numerator = false)
    {
        Expr shortest = e.Simplify(assumptions);
        if (shortest.GetVariables().Count == 0)
            return shortest;

        Expr[] others = numerator
            ? [e.Expand(SimplifyMode.Strict).Simplify(assumptions), Together(shortest).Numerator.Simplify(assumptions)]
            : [e.Expand(SimplifyMode.Strict).Simplify(assumptions)];
        foreach (Expr other in others)
        {
            if (other.Print().Length < shortest.Print().Length)
                shortest = other;
        }

        return shortest;
    }

    // e compared with 0 as something simpler compared with 0, or null: a for 2a or a / 2 (b for
    // -2b, the comparison flipped), a for a^3, a > 0 for 1 / a >= 0, sqrt(a) > 0 and a^p > 0, a != 0
    // for a^2 > 0.
    private Expr? Peeled(Expr e, ref Comparison comparison)
    {
        bool equality = comparison is Comparison.Equal or Comparison.NotEqual;
        int? SignOfNumber(Expr number) => number.GetVariables().Count == 0 && SignOf(number) is int s and not 0 ? s : null;

        switch (e)
        {
            case Negate negation:
                comparison = Flipped(comparison);
                return negation.Argument;
            case Multiply product when SignOfNumber(product.Left) is int sign:
                return Signed(product.Right, sign, ref comparison);
            case Multiply product when SignOfNumber(product.Right) is int sign:
                return Signed(product.Left, sign, ref comparison);
            case Divide quotient when SignOfNumber(quotient.Denominator) is int sign:
                return Signed(quotient.Numerator, sign, ref comparison);
            case Divide { Numerator: Multiply product } quotient when SignOfNumber(product.Left) is int sign:
                return Signed(product.Right / quotient.Denominator, sign, ref comparison);
            case Divide { Denominator: Multiply product } quotient when SignOfNumber(product.Left) is int sign:
                return Signed(quotient.Numerator / product.Right, sign, ref comparison);
            case Divide quotient when !equality && SignOfNumber(quotient.Numerator) is int sign:
                comparison = comparison switch { Comparison.GreaterOrEqual => Comparison.Greater, Comparison.LessOrEqual => Comparison.Less, var same => same };
                return Signed(quotient.Denominator, sign, ref comparison);
            case Divide quotient when equality && quotient.Numerator.GetVariables().Count > 0:
                return quotient.Numerator;
            case Power { Exponent: Constant { Value: { IsInteger: true } n } } power when n.Sign > 0:
                return Powered(power.Base, n.Numerator.IsEven, root: false, ref comparison);
            case Power { Exponent: not Constant { Value.IsInteger: true } } power when comparison is Comparison.Greater or Comparison.NotEqual:
                return power.Base;
            case Sqrt root:
                return Powered(root.Argument, even: true, root: true, ref comparison);
            case NthRoot { Degree: Constant { Value: { IsInteger: true } n } } root when n.Sign > 0:
                return Powered(root.Argument, n.Numerator.IsEven, root: true, ref comparison);
            case Abs absolute:
                return Powered(absolute.Argument, even: true, root: false, ref comparison);
            default:
                return e.GetVariables().Count > 0 && SquareRoot(e) is { } square ? Powered(square, even: true, root: false, ref comparison) : null;
        }

        static Expr Signed(Expr inner, int sign, ref Comparison comparison)
        {
            if (sign < 0)
                comparison = Flipped(comparison);
            return inner;
        }

        // An odd power or root has the sign of its base. An even one is 0 where the base is, and
        // positive elsewhere: where the base is defined for a root, anywhere else for a power.
        static Expr? Powered(Expr inner, bool even, bool root, ref Comparison comparison)
        {
            if (!even)
                return inner;
            Comparison? replaced = comparison switch
            {
                Comparison.Equal or Comparison.LessOrEqual => Comparison.Equal,
                Comparison.NotEqual => Comparison.NotEqual,
                Comparison.Greater => root ? Comparison.Greater : Comparison.NotEqual,
                _ => null,
            };
            if (replaced is not { } kept)
                return null;
            comparison = kept;
            return inner;
        }
    }

    // The conditions once each, a product that isn't 0 as factors that aren't and abs(a - 1) <= 2
    // as a >= -1 and a <= 3, those with the same two sides combined, a > 0 for a >= 0 and a != 0,
    // of two bounds on one side by numbers the tighter, a > 1 for a > 1 and a > 0, and without
    // those others imply, a != 0 next to abs(a) >= 1.
    private List<Condition> Merged(IEnumerable<Condition> conditions)
    {
        IEnumerable<Condition> Split(Condition condition)
        {
            IEnumerable<Condition> parts = condition switch
            {
                { Left: Multiply product, Comparison: Comparison.NotEqual, Right: Constant { Value.IsZero: true } } =>
                    Factors(product).Select(factor => new Condition(factor, Comparison.NotEqual, 0)),
                { Left: Abs absolute, Comparison: Comparison.LessOrEqual or Comparison.Less, Right: var bound } when bound.GetVariables().Count == 0 =>
                    [
                        new(absolute.Argument, condition.Comparison == Comparison.Less ? Comparison.Greater : Comparison.GreaterOrEqual, (-bound).Simplify(assumptions)),
                        new(absolute.Argument, condition.Comparison, bound),
                    ],
                _ => [],
            };
            return parts.Any() ? parts.Where(part => Decide(part) != true).Select(Tidied) : [condition];
        }

        var merged = new List<Condition>();
        foreach (Condition condition in conditions.SelectMany(Split))
        {
            int i = merged.FindIndex(other => other.Left.Equals(condition.Left) && other.Right.Equals(condition.Right));
            if (i >= 0 && Combined(merged[i].Comparison, condition.Comparison) is { } both)
            {
                merged[i] = new Condition(condition.Left, both, condition.Right);
                continue;
            }

            int direction = Direction(condition.Comparison);
            double bound = direction == 0 ? double.NaN : Bound(condition);
            int j = merged.FindIndex(other => other.Left.Equals(condition.Left) && direction != 0 && Direction(other.Comparison) == direction
                && double.IsFinite(bound) && double.IsFinite(Bound(other)));
            if (j < 0)
                merged.Add(condition);
            else if (direction * bound.CompareTo(Bound(merged[j])) is var tighter && (tighter > 0 || tighter == 0 && condition.Comparison is Comparison.Greater or Comparison.Less))
                merged[j] = condition;
        }

        // Over the complex numbers there are no bounds, and a number like i has no double.
        if (!complex)
            merged.RemoveAll(condition => condition is { Comparison: Comparison.NotEqual } && condition.Right.GetVariables().Count == 0
                && merged.Any(other => !ReferenceEquals(other, condition) && Excludes(other, condition.Left, condition.Right.Evaluate())));
        return merged;

        // Whether the condition fails where u = c.
        static bool Excludes(Condition other, Expr u, double c)
        {
            if (other.Comparison == Comparison.NotEqual || other.Right.GetVariables().Count > 0)
                return false;
            if (other.Left is Abs absolute && absolute.Argument.Equals(u))
                c = Math.Abs(c);
            else if (!other.Left.Equals(u))
                return false;

            double bound = other.Right.Evaluate();
            return !(other.Comparison switch
            {
                Comparison.Equal => c == bound,
                Comparison.Less => c < bound,
                Comparison.LessOrEqual => c <= bound,
                Comparison.Greater => c > bound,
                _ => c >= bound,
            });
        }

        static int Direction(Comparison comparison) => comparison switch
        {
            Comparison.Greater or Comparison.GreaterOrEqual => 1,
            Comparison.Less or Comparison.LessOrEqual => -1,
            _ => 0,
        };

        static double Bound(Condition condition) => condition.Right.GetVariables().Count == 0 ? condition.Right.Evaluate() : double.NaN;
    }

    private static Comparison? Combined(Comparison a, Comparison b)
    {
        if (a == b)
            return a;
        return (a, b) switch
        {
            (Comparison.GreaterOrEqual or Comparison.Greater, Comparison.NotEqual or Comparison.Greater or Comparison.GreaterOrEqual)
                or (Comparison.NotEqual, Comparison.GreaterOrEqual or Comparison.Greater) => Comparison.Greater,
            (Comparison.LessOrEqual or Comparison.Less, Comparison.NotEqual or Comparison.Less or Comparison.LessOrEqual)
                or (Comparison.NotEqual, Comparison.LessOrEqual or Comparison.Less) => Comparison.Less,
            _ => null,
        };
    }

    private void SolveFactor(Expr factor, List<Solution> candidates, List<Expr> unsolved)
    {
        if (Polynomials.Coefficients(factor, variable, MaxDegree) is not { Length: >= 2 } coefficients)
        {
            unsolved.Add(factor);
            return;
        }

        // x a + b as x (a x + b), so that 0 doesn't take the condition a != 0.
        if (coefficients.Length > 2 && coefficients[0] is Constant { Value.IsZero: true })
        {
            candidates.Add(new Solution(0, []));
            SolveFactor(Polynomials.Of(coefficients[1..], variable), candidates, unsolved);
            return;
        }

        switch (coefficients.Length - 1)
        {
            case 1 when Needed(new Condition(coefficients[1], Comparison.NotEqual, 0)) is { } needs:
                candidates.Add(new Solution((-coefficients[0] / coefficients[1]).Simplify(assumptions), needs));
                return;
            case 2 when Quadratic(coefficients[0], coefficients[1], coefficients[2]) is { } roots:
                candidates.AddRange(roots);
                if (numeric && roots.Count == 2 && coefficients.All(coefficient => coefficient.GetVariables().Count == 0))
                    foreach (Solution root in roots)
                        _quadratics[root.Value] = [.. coefficients.Select(coefficient => coefficient.EvaluateComplex())];
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
                candidates.AddRange(roots.Select(root => new Solution(root, [])));
                return;
            }
        }

        if (numeric && Approximately(coefficients) is { } approximations)
            candidates.AddRange(approximations.Select(root => new Solution(root, [])));
        else
            unsolved.Add(factor);
    }

    // What a step of solving needs that isn't decided, tidied; null where a need fails, or where a
    // number can't be told to meet it.
    private List<Condition>? Needed(params Condition[] needs)
    {
        var open = new List<Condition>();
        foreach (Condition need in needs)
        {
            bool? holds = Decide(need);
            if (holds == false || holds is null && need.Left.GetVariables().Count == 0 && need.Right.GetVariables().Count == 0)
                return null;
            if (holds is null && Tidied(need) is var tidied && !open.Contains(tidied))
                open.Add(tidied);
        }

        return open;
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
            roots.AddRange(pair.Select(root => root.Value));
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

    // a x^2 + b x + c = 0 by the discriminant, or null where a number's sign isn't known.
    private List<Solution>? Quadratic(Expr c, Expr b, Expr a)
    {
        if (Needed(new Condition(a, Comparison.NotEqual, 0)) is not { } needs)
            return null;
        if (a.Print().StartsWith('-'))
            (a, b, c) = ((-a).Simplify(assumptions), (-b).Simplify(assumptions), (-c).Simplify(assumptions));

        // The centre and the distance to each root apart, so that their numbers cancel:
        // -1 + sqrt(2), not (-2 + 2sqrt(2)) / 2.
        Expr discriminant = Shortest(b * b - 4 * a * c);
        Expr centre = (-b / (2 * a)).Simplify(assumptions);
        Expr Half(Expr d) => (new Sqrt(d) / (2 * a)).Simplify(assumptions);
        List<Solution> Pair(Expr low, Expr high, List<Condition> conditions) =>
            [new(low.Simplify(assumptions), conditions), new(high.Simplify(assumptions), conditions)];
        switch (SignOf(discriminant))
        {
            case > 0:
                return Pair(centre - Half(discriminant), centre + Half(discriminant), needs);
            case 0:
                return [new(centre, needs)];
            case < 0 when complex:
                return Pair(centre - Expr.I * Half(-discriminant), centre + Expr.I * Half(-discriminant), needs);
            case < 0:
                return [];
        }

        if (discriminant.GetVariables().Count == 0)
            return null;

        // With parameters, a discriminant that is a square needs no abs: x^2 - (a + b) x + a b = 0
        // at a and b, expanded where Simplify leaves (a + b) / 2 - (a - b) / 2.
        if (SquareRoot(discriminant) is { } root)
        {
            Expr half = (root / (2 * a)).Simplify(assumptions);
            Expr Shortest(Expr value)
            {
                Expr simplified = value.Simplify(assumptions), expanded = value.Expand(SimplifyMode.Strict).Simplify(assumptions);
                return expanded.Print().Length < simplified.Print().Length ? expanded : simplified;
            }

            return [new(Shortest(centre - half), needs), new(Shortest(centre + half), needs)];
        }

        // The formula as it is written, or the centre and the root of what is left, whichever is
        // shorter: 1 - sqrt(1 - a) for x^2 - 2x + a = 0, sqrt(2E / m) for E = m v^2 / 2.
        Expr reduced = Shortest(centre * centre - c / a);
        Expr formula = ((-b - new Sqrt(discriminant)) / (2 * a)).Simplify(assumptions);
        bool centred = (centre - new Sqrt(reduced)).Simplify(assumptions).Print().Length < formula.Print().Length;
        Expr radicand = centred ? reduced : discriminant;

        Condition nonNegative = new(radicand, Comparison.GreaterOrEqual, 0);
        if (!complex && Decide(nonNegative) == false)
            return [];
        List<Condition>? real = complex ? [] : Needed(nonNegative);
        if (real is null)
            return null;
        return centred
            ? Pair(centre - new Sqrt(radicand), centre + new Sqrt(radicand), [.. needs, .. real])
            : Pair((-b - new Sqrt(radicand)) / (2 * a), (-b + new Sqrt(radicand)) / (2 * a), [.. needs, .. real]);
    }

    // s with s^2 = d, by d as a square of a polynomial in one of its variables, the coefficient of
    // its square a square too: a - b for a^2 - 2a b + b^2; null if it isn't seen to be one.
    private Expr? SquareRoot(Expr d)
    {
        if (d.GetVariables().Count == 0)
            return SignOf(d) >= 0 && new Sqrt(d).Simplify(assumptions) is Constant root ? root : null;

        foreach (string name in d.GetVariables().Order(StringComparer.Ordinal))
        {
            if (Polynomials.Coefficients(d, name, 2) is not { Length: 3 } k
                || (k[1] * k[1] - 4 * k[2] * k[0]).Expand(SimplifyMode.Strict) is not Constant { Value.IsZero: true }
                || SquareRoot(k[2]) is not { } leading)
                continue;
            return (leading * (new Variable(name) + k[1] / (2 * k[2]))).Simplify(assumptions);
        }

        return null;
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

    // The candidate as a solution, with the conditions on it that aren't decided; nothing where
    // one fails. Of a family, the values that meet them: π * k with x != 0 leaves k != 0.
    private IEnumerable<Solution> Admitted(Solution candidate, IReadOnlyList<Condition> conditions)
    {
        var open = new List<Condition>(candidate.Conditions);
        for (int i = 0; i < conditions.Count; i++)
        {
            Condition condition = conditions[i];
            var at = new Condition(
                condition.Left.Substitute(variable, candidate.Value).Simplify(assumptions),
                condition.Comparison,
                condition.Right.Substitute(variable, candidate.Value).Simplify(assumptions));
            bool? holds = Decide(at);
            if (holds == false)
                yield break;
            if (holds == true)
                continue;

            // A family split into the parts that meet the condition, which take the rest.
            if (candidate.Index is not null && OnIndex(at) && IndexBound(at) is (null, null)
                && (Residues(condition, candidate, open) ?? Partitioned(condition, candidate, open)) is { } parts)
            {
                foreach (Solution part in parts)
                {
                    foreach (Solution solution in Admitted(part, [.. conditions.Skip(i + 1)]))
                        yield return solution;
                }

                yield break;
            }

            // Tidied, the assumptions may decide it: sqrt(a) > 0 is a > 0, which a positive a meets.
            Condition tidied = Tidied(at);
            if (Decide(tidied) is { } decided)
            {
                if (!decided)
                    yield break;
                continue;
            }

            if (!open.Contains(tidied))
                open.Add(tidied);
        }

        List<Condition> merged = Merged(open);
        if (candidate.Index is null)
            yield return new Solution(Plain(candidate.Value, merged), merged);
        else if (Normalized(candidate.Start, candidate.Period!, merged) is { } family)
            yield return family;
    }

    // A value with what a condition sets a parameter to put in, 0 for sqrt(-a^2) if a = 0, and
    // as a square where that is shorter: (a - 1)^2 / 4 for (a^2 - 2a + 1) / 4.
    private Expr Plain(Expr value, List<Condition> conditions)
    {
        foreach (Condition condition in conditions)
        {
            if (condition is { Left: Variable { Name: var name }, Comparison: Comparison.Equal } && condition.Right.GetVariables().Count == 0)
                value = value.Substitute(name, condition.Right).Simplify(assumptions);
        }

        if (value.GetVariables().Count == 0 || value is Power || SquareRoot(value) is not { } root)
            return value;
        Expr square = (root switch
        {
            Divide { Denominator: Constant } quotient => new Power(quotient.Numerator, 2) / (quotient.Denominator * quotient.Denominator),
            Multiply { Left: Constant } product => product.Left * product.Left * new Power(product.Right, 2),
            _ => new Power(root, 2),
        }).Simplify(assumptions);
        return square.Print().Length < value.Print().Length ? square : value;
    }

    // An equation inside another leaves them to the outer one.
    private IEnumerable<Condition> SignAssumed()
    {
        if (complex || outer is not null)
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
        if (OnIndex(condition) && IndexBound(condition) is (var holds, var bound) && (holds is not null || bound is not null))
            return holds;

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
                _ => ByForm(difference, condition.Comparison),
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
            _ => ByForm(difference, condition.Comparison),
        };
    }

    // What the form of a difference with parameters decides: (a b + 1) / b - a, 1 / b over a
    // common denominator, is never 0, and a^2 - 2a + 1, a square, never negative.
    private bool? ByForm(Expr difference, Comparison comparison)
    {
        if (difference.GetVariables().Count == 0)
            return null;
        if (comparison is Comparison.Equal or Comparison.NotEqual)
        {
            Expr numerator = Together(difference).Numerator.Simplify(assumptions);
            return !numerator.Equals(difference) && IsNonZero(numerator) ? comparison == Comparison.NotEqual : null;
        }

        if (comparison is Comparison.GreaterOrEqual or Comparison.Less && SquareRoot(difference) is not null)
            return comparison == Comparison.GreaterOrEqual;
        if (comparison is Comparison.LessOrEqual or Comparison.Greater && SquareRoot((-difference).Simplify(assumptions)) is not null)
            return comparison == Comparison.LessOrEqual;
        return null;
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
