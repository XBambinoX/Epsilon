using Epsilon.Core;

namespace Epsilon.Solving;

// Equations that are polynomials in one part of the unknown, as sin(x)^2 - 3 sin(x) + 2 = 0 in
// sin(x): solved for that part, t^2 - 3t + 2 = 0 at 1 and 2, and then sin(x) = 1 and sin(x) = 2.
// Several parts that are powers of one become that one first: 4^x - 3 * 2^x + 2 in t = 2^x is
// t^2 - 3t + 2, and x - 3 sqrt(x) + 2 in t = sqrt(x) too.
internal sealed partial class Solver
{
    // Stands for the part, a name no equation has.
    private const string Part = "#part";

    // The solutions of factor = 0 as a polynomial in one part of the unknown, and the part; null
    // where there is no such part, or a solution isn't found completely.
    private (List<Solution> Solutions, Expr Kernel)? Substituted(Expr factor)
    {
        // Of degree 1 in a part that was there already, kernel = t would come back here.
        if (Unified(factor) is not var (polynomial, kernel, unified)
            || Polynomials.Coefficients(polynomial, Part, MaxDegree) is not { Length: var length } || length < (unified ? 2 : 3))
            return null;

        SolutionSet values = new Solver(Part, assumptions, complex, numeric, region: null, this).Solve(polynomial, 0);
        if (!values.IsComplete || values.IsEveryValue)
            return null;

        var solutions = new List<Solution>();
        foreach (Solution value in values.Solutions)
        {
            SolutionSet found = new Solver(variable, assumptions, complex, numeric, region, this).Solve(kernel, value.Value);
            if (!found.IsComplete || found.IsEveryValue)
                return null;
            solutions.AddRange(found.Solutions.Select(solution => solution.With(Merged([.. solution.Conditions, .. value.Conditions]))));
        }

        // A value of the part found numerically is a Constant, which keeps 15 digits: the solutions
        // worked out from it get theirs back on the factor.
        if (numeric && values.Solutions.Any(value => _numbers.ContainsKey(value.Value)))
        {
            Func<double, double> polished = RealRoots.Polisher(factor, variable, many: false);
            for (int i = 0; i < solutions.Count; i++)
            {
                if (solutions[i].Index is not null)
                    continue;
                double x = polished(Number(solutions[i].Value).Real);
                Expr number = x;
                _numbers[number] = x;
                solutions[i] = new Solution(number, solutions[i].Conditions);
            }
        }

        return (solutions, kernel);
    }

    // factor as a polynomial in Part, the part it stands for, and whether several parts, or the
    // unknown itself, were brought to it.
    private (Expr Polynomial, Expr Kernel, bool Unified)? Unified(Expr factor)
    {
        var (parts, bare) = Parts(factor);
        Expr kernel;
        Dictionary<Expr, Expr> replacements;
        Expr? unknown = null;
        if (parts.Count == 1 && !bare)
            (kernel, replacements) = (parts[0], new Dictionary<Expr, Expr> { [parts[0]] = new Variable(Part) });
        else if (!bare && Exponentials(parts) is var (exponential, powers))
            (kernel, replacements) = (exponential, powers);
        else if (Roots(parts, bare) is var (root, roots, alone))
            (kernel, replacements, unknown) = (root, roots, alone);
        else
            return null;

        Expr Replaced(Expr e) =>
            replacements.TryGetValue(e, out Expr? replacement) ? replacement
            : e is Variable { Name: var name } && name == variable ? unknown ?? e
            : e.Children.Length == 0 ? e : e.MapChildren(Replaced);
        Expr replaced = Replaced(factor);
        if (replaced.DependsOn(variable))
            return null;

        bool unified = parts.Count > 1 || bare;
        return (unified ? Together(replaced).Numerator : replaced, kernel, unified);
    }

    // The parts of expr that the unknown is in, past sums, products and whole powers: sin(x) in
    // sin(x)^2 - 3 sin(x) + 2; and whether it is outside them too, as in sin(x) + x.
    private (List<Expr> Parts, bool Bare) Parts(Expr expr)
    {
        var parts = new List<Expr>();
        bool bare = false;
        void Walk(Expr e)
        {
            if (!e.DependsOn(variable))
                return;

            switch (e)
            {
                case Add or Subtract or Multiply or Negate:
                    foreach (Expr child in e.Children)
                        Walk(child);
                    break;
                case Divide quotient when !quotient.Denominator.DependsOn(variable):
                    Walk(quotient.Numerator);
                    break;
                case Power { Exponent: Constant { Value: { IsInteger: true } n } } power when n.Sign > 0:
                    Walk(power.Base);
                    break;
                case Variable:
                    bare = true;
                    break;
                default:
                    if (!parts.Contains(e))
                        parts.Add(e);
                    break;
            }
        }

        Walk(expr);
        return (parts, bare);
    }

    // Exponentials of one base as powers of one of them, numbers in their exponents as factors:
    // in t = 2^x, 4^x is t^2 and 2^(x + 1) is 2t; in t = exp(x), exp(-x) is 1 / t.
    private (Expr Kernel, Dictionary<Expr, Expr> Replacements)? Exponentials(List<Expr> parts)
    {
        // The base, null for e, and each exponent split into its part with the unknown and the rest.
        Expr? common = null;
        bool first = true;
        var exponents = new List<(Expr Part, Expr Free)>();
        foreach (Expr part in parts)
        {
            Expr? b;
            Expr exponent;
            switch (part)
            {
                case Exp e:
                    (b, exponent) = (null, e.Argument);
                    break;
                case Power power when !power.Base.DependsOn(variable):
                    (b, exponent) = (power.Base, power.Exponent);
                    break;
                default:
                    return null;
            }

            // 4^x as 2^(2x).
            Rational scale = 1;
            if (b is Constant { Value: var number } && PerfectPower(number) is var (smallest, times))
                (b, scale) = (smallest, times);
            if (first)
                (common, first) = (b, false);
            else if (!Equals(common, b))
                return null;
            exponents.Add(Split((new Constant(scale) * exponent).Expand()));
        }

        // Each exponent's part a whole multiple of the kernel's: 2x and -x of x, x of x / 2.
        var ratios = new List<Rational>();
        foreach (var (part, _) in exponents)
        {
            if ((part / exponents[0].Part).Expand().Simplify(assumptions) is not Constant { Value: { IsZero: false } ratio })
                return null;
            ratios.Add(ratio);
        }

        System.Numerics.BigInteger lcm = ratios.Aggregate(System.Numerics.BigInteger.One, (l, r) => l / System.Numerics.BigInteger.GreatestCommonDivisor(l, r.Denominator) * r.Denominator);
        System.Numerics.BigInteger gcd = ratios.Aggregate(System.Numerics.BigInteger.Zero, (g, r) => System.Numerics.BigInteger.GreatestCommonDivisor(g, r.Numerator * (lcm / r.Denominator)));
        var step = new Rational(gcd, lcm);

        Expr Raised(Expr exponent) => common is null ? new Exp(exponent) : new Power(common, exponent);
        Expr kernel = Raised((new Constant(step) * exponents[0].Part).Simplify(assumptions));
        var t = new Variable(Part);
        var replacements = new Dictionary<Expr, Expr>();
        for (int i = 0; i < parts.Count; i++)
        {
            Expr factor = exponents[i].Free is Constant { Value.IsZero: true } ? 1 : Raised(exponents[i].Free);
            replacements[parts[i]] = factor * new Power(t, new Constant(ratios[i] / step));
        }

        return (kernel, replacements);
    }

    // Roots of one radicand as powers of one root, and the unknown, where the radicand is linear
    // in it, as that root's power: in t = sqrt(x), x is t^2; in t = nthroot(x, 3), x^(2/3) is t^2.
    private (Expr Kernel, Dictionary<Expr, Expr> Replacements, Expr? Unknown)? Roots(List<Expr> parts, bool bare)
    {
        Expr? radicand = null;
        var exponents = new List<Rational>();
        foreach (Expr part in parts)
        {
            (Expr? u, Rational q) = part switch
            {
                Sqrt root => (root.Argument, new Rational(1, 2)),
                NthRoot { Degree: Constant { Value: { IsInteger: true } n } } root when n > 1 => (root.Argument, new Rational(1, n.Numerator)),
                Power { Exponent: Constant { Value: { IsInteger: false } exponent } } power => (power.Base, exponent),
                _ => ((Expr?)null, Rational.Zero),
            };
            if (u is null || radicand is not null && !radicand.Equals(u))
                return null;
            radicand = u;
            exponents.Add(q);
        }

        if (radicand is null)
            return null;
        System.Numerics.BigInteger degree = exponents.Aggregate(System.Numerics.BigInteger.One, (l, q) => l / System.Numerics.BigInteger.GreatestCommonDivisor(l, q.Denominator) * q.Denominator);
        if (degree > MaxDegree)
            return null;

        var t = new Variable(Part);
        Expr kernel = degree == 2 ? new Sqrt(radicand) : new NthRoot(radicand, (int)degree);
        var replacements = new Dictionary<Expr, Expr>();
        for (int i = 0; i < parts.Count; i++)
            replacements[parts[i]] = new Power(t, new Constant(exponents[i] * new Rational(degree)));

        // x = (t^n - b) / a where the radicand is a x + b.
        Expr? unknown = null;
        if (bare)
        {
            if (Polynomials.Coefficients(radicand, variable, 1) is not [var b, var a] || !IsNonZero(a))
                return null;
            unknown = (new Power(t, (int)degree) - b) / a;
        }

        return (kernel, replacements, unknown);
    }

    // The part of e with the unknown in it, and the rest: (2x, 1) for 2x + 1.
    private (Expr Part, Expr Free) Split(Expr e)
    {
        (Expr, Expr) Of(Expr term) => term switch
        {
            Add sum when Of(sum.Left) is var (a, b) && Of(sum.Right) is var (c, d) => (a + c, b + d),
            Subtract difference when Of(difference.Left) is var (a, b) && Of(difference.Right) is var (c, d) => (a - c, b - d),
            Negate negation when Of(negation.Argument) is var (a, b) => (-a, -b),
            _ when term.DependsOn(variable) => (term, 0),
            _ => (0, term),
        };

        var (part, rest) = Of(e);
        return (part.Simplify(assumptions), rest.Simplify(assumptions));
    }

    // A rational number above 0 as root^power with the smallest root: (2, 2) for 4, (2, -3) for
    // 1/8; null for 1, which has no such root, and for numbers below 0.
    private static (Expr Root, Rational Power)? PerfectPower(Rational number)
    {
        if (number.Sign <= 0 || number.IsOne)
            return null;
        if (number < 1)
            return PerfectPower(1 / number) is var (root, power) ? (root, -power) : null;

        for (int k = 64; k >= 2; k--)
        {
            if (Integer(number.Numerator, k) is { } top && Integer(number.Denominator, k) is { } bottom)
                return (new Constant(new Rational(top, bottom)), k);
        }

        return (new Constant(number), 1);

        static System.Numerics.BigInteger? Integer(System.Numerics.BigInteger n, int k)
        {
            var guess = new System.Numerics.BigInteger(Math.Round(Math.Exp(System.Numerics.BigInteger.Log(n) / k)));
            for (System.Numerics.BigInteger r = System.Numerics.BigInteger.Max(guess - 1, 1); r <= guess + 1; r++)
            {
                if (System.Numerics.BigInteger.Pow(r, k) == n)
                    return r;
            }

            return null;
        }
    }
}
