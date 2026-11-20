using Epsilon.Core;

namespace Epsilon.Solving;

// Equations with roots of the unknown in them, as sqrt(x) + sqrt(x + 1) = 3: each root on one
// side of its own and both sides raised to its degree, until a polynomial is left. Squaring can add
// solutions: sqrt(x + 1) = x - 1 squared is x + 1 = (x - 1)^2, solved by 0 too, where the sides are
// 1 and -1. An even root is never negative, which keeps them out as a condition: x - 1 >= 0.
internal sealed partial class Solver
{
    // Stands for the root being taken out, a name no equation has.
    private const string Root = "#root";

    private const int MaxRoots = 6;

    // The solutions of factor = 0 with the conditions that keep out those raising to a power
    // added; null where a root can't be taken out, as in sin(sqrt(x)), or the polynomial left isn't
    // solved completely.
    private List<(Solution Solution, List<Condition> Needs)>? Rationalized(Expr factor)
    {
        var needs = new List<Condition>();
        Expr rest = factor;
        for (int taken = 0; OutermostRoot(rest) is var (argument, degree); taken++)
        {
            if (taken == MaxRoots || TakenOut(rest, argument, degree) is not var (a, b))
                return null;

            // a + b * root = 0, so (-a)^n = b^n * argument; an even root is >= 0, so -a * b >= 0.
            rest = (new Power(-a, degree) - new Power(b, degree) * argument).Expand(SimplifyMode.Strict);
            if (degree % 2 == 0)
                needs.Add(new Condition((a * b).Simplify(assumptions), Comparison.LessOrEqual, 0));
        }

        if (Polynomials.Coefficients(rest, variable, MaxDegree) is null)
            return null;
        SolutionSet solved = new Solver(variable, assumptions, complex, numeric, region, this).Solve(rest, 0);
        if (!solved.IsComplete || solved.IsEveryValue)
            return null;

        // Numbers found numerically are checked on the factor itself: the core can't tell the sign
        // of sqrt(600499750269605) / 5000000 - 4, which has numbers that large in it.
        if (numeric)
            return [.. solved.Solutions.Where(solution => Vanishes(factor, Number(solution.Value).Real)).Select(solution => (solution, new List<Condition>()))];
        return [.. solved.Solutions.Select(solution => (solution, needs))];
    }

    // Whether f(x) is 0 up to rounding, next to the size of the terms it sums. Loose, as near
    // the edge of a root's domain, sqrt(1e-16), an error of an ulp in x is 1e-8 in f.
    private bool Vanishes(Expr f, double x)
    {
        double At(Expr e)
        {
            try
            {
                return e.Evaluate((variable, x));
            }
            catch
            {
                return double.NaN;
            }
        }

        double Size(Expr e) => e switch
        {
            Add sum => Size(sum.Left) + Size(sum.Right),
            Subtract difference => Size(difference.Left) + Size(difference.Right),
            Negate negation => Size(negation.Argument),
            Multiply product => Size(product.Left) * Size(product.Right),
            Divide quotient => Size(quotient.Numerator) / Math.Abs(At(quotient.Denominator)),
            _ => Math.Abs(At(e)),
        };

        double value = At(f);
        return double.IsFinite(value) && Math.Abs(value) <= 1e-6 * Size(f);
    }

    // The argument and degree of a root of the unknown not inside another one.
    private (Expr Argument, int Degree)? OutermostRoot(Expr e) => e switch
    {
        Sqrt root when root.Argument.DependsOn(variable) => (root.Argument, 2),
        NthRoot { Degree: Constant { Value: { IsInteger: true } n } } root when n > 1 && n <= MaxDegree && root.Argument.DependsOn(variable) =>
            (root.Argument, (int)n.Numerator),
        Power { Exponent: Constant { Value: { IsInteger: false } q } } power when q.Denominator <= MaxDegree && power.Base.DependsOn(variable) =>
            (power.Base, (int)q.Denominator),
        _ => e.Children.Select(OutermostRoot).FirstOrDefault(found => found is not null),
    };

    // expr as a + b * root, root the degree-th root of argument and a and b without it; null where
    // expr isn't a polynomial in the root, or the root's square and higher powers below the degree
    // are left in it, as in cbrt(x)^2 + cbrt(x) = 2.
    private (Expr A, Expr B)? TakenOut(Expr expr, Expr argument, int degree)
    {
        Expr Replaced(Expr e) => e switch
        {
            Sqrt root when degree == 2 && root.Argument.Equals(argument) => new Variable(Root),
            NthRoot { Degree: Constant { Value: var n } } root when n == degree && root.Argument.Equals(argument) => new Variable(Root),
            Power { Exponent: Constant { Value: { IsInteger: false } q } } power when q.Denominator == degree && power.Base.Equals(argument) =>
                new Power(new Variable(Root), new Constant(new Rational(q.Numerator))),
            _ => e.Children.Length == 0 ? e : e.MapChildren(Replaced),
        };

        if (Polynomials.Coefficients(Replaced(expr), Root, MaxDegree) is not { } coefficients)
            return null;

        // root^degree is the argument.
        var reduced = new Expr[degree];
        Array.Fill(reduced, new Constant(0));
        for (int j = 0; j < coefficients.Length; j++)
            reduced[j % degree] += coefficients[j] * new Power(argument, j / degree);
        reduced = [.. reduced.Select(coefficient => coefficient.Simplify(assumptions, SimplifyMode.Strict))];

        if (reduced.Skip(2).Any(coefficient => coefficient is not Constant { Value.IsZero: true }))
            return null;
        return (reduced[0], degree > 1 ? reduced[1] : 0);
    }
}
