using Epsilon.Core;

namespace Epsilon.Calculus;

// Limits as x -> inf by Gruntz's algorithm (D. Gruntz, On Computing Limits in a Symbolic
// Manipulation System, 1996). The subexpressions that grow or vanish fastest, the mrv set, are
// written as powers of one of them, w -> 0+, with the rest as constants; then the expression is
// c w^e (1 + o(1)), and its limit is 0 for e > 0, an infinity of the sign of c for e < 0, and the
// limit of c, which varies slower, for e = 0.
internal sealed class Gruntz(string variable)
{
    private const int MaxDepth = 64;

    private readonly Variable _x = new(variable);
    private readonly Dictionary<Expr, Limit> _limits = [];
    private readonly Dictionary<Expr, int> _signs = [];
    private int _depth, _names;

    public Limit AtInfinity(Expr e)
    {
        if (!e.DependsOn(variable))
            return Limit.Finite(WithE(SeriesExpander.Tidy(e)));
        if (_limits.TryGetValue(e, out Limit? known))
            return known;
        try
        {
            if (++_depth > MaxDepth)
                throw new NotSupportedException($"The limit of {e.Print()} nests more than {MaxDepth} limits deep.");

            Limit limit;
            try
            {
                limit = FromLeadingTerm(Prepare(e));
            }
            catch (Oscillation)
            {
                limit = Squeezed(e) ?? throw new Oscillation();
            }

            _limits[e] = limit;
            return limit;
        }
        finally
        {
            _depth--;
        }
    }

    private Limit FromLeadingTerm(Expr e)
    {
        var (c, exponent) = LeadingTerm(e);
        if (exponent > 0)
            return Limit.Finite(0);
        if (exponent == 0)
            return AtInfinity(c);

        int sign = SignOf(c);
        return sign != 0 ? Limit.Infinite(sign) : throw new NotSupportedException($"The leading coefficient {c.Print()} of {e.Print()} is 0.");
    }

    // The sign of e for every x large enough.
    private int SignOf(Expr e)
    {
        if (!e.DependsOn(variable))
            return ConstantSign(e);
        if (_signs.TryGetValue(e, out int known))
            return known;

        int sign = e switch
        {
            Variable or Exp => 1,
            Negate negate => -SignOf(negate.Argument),
            Multiply product => SignOf(product.Left) * SignOf(product.Right),
            Divide quotient => SignOf(quotient.Numerator) * SignOf(quotient.Denominator),
            Power { Exponent: Constant { Value: { IsInteger: true } k } } power => k.Numerator.IsEven ? Math.Abs(SignOf(power.Base)) : SignOf(power.Base),
            _ => SignOf(LeadingTerm(Prepare(e)).Coefficient),
        };

        _signs[e] = sign;
        return sign;
    }

    private static int ConstantSign(Expr c)
    {
        Expr simplified = c.Simplify();
        if (simplified is Constant constant)
            return constant.Value.Sign;
        if (simplified.GetVariables().Count > 0)
            throw new UnknownSign(simplified);
        if (ComplexParts.HasImaginaryUnit(simplified))
            throw new NotSupportedException($"{simplified.Print()} isn't real.");

        double value = simplified.Evaluate();
        return double.IsNaN(value) ? throw new NotSupportedException($"{simplified.Print()} isn't a number.") : Math.Sign(value);
    }

    // The coefficient and exponent of the lowest term c w^e of e with its mrv set written in w.
    private (Expr Coefficient, Rational Exponent) LeadingTerm(Expr e)
    {
        List<Expr> omega = Mrv(e);
        if (omega.Count == 0)
            return (e, 0);

        List<Variable> names = [.. omega.Select(_ => Fresh())];
        Expr named = Replace(e, omega, names);
        List<Expr> arguments = [.. omega.Select(element => element is Exp exp ? Replace(exp.Argument, omega, names) : element)];

        // x itself in the set: x -> exp(x) moves every element a level up, x to exp(x), so
        // that all of them are exponentials. Below, x varies slower than they do.
        List<Expr> elements = omega;
        if (omega.IndexOf(_x) is var level and >= 0)
        {
            elements = [.. omega.Select(element => element.Substitute(variable, new Exp(_x)).Simplify())];
            arguments[level] = _x;
        }

        if (elements.Any(element => element is not Exp))
            throw new NotSupportedException($"{e.Print()} isn't written in exponentials of {variable}.");

        // w is g or 1/g for an element g, at first one with no other in its argument, and every
        // element f left is exp(f - c g) g^c for the rational c its argument grows at relative to
        // that of g. An element left only in the arguments of others needs no c, so where one
        // would be irrational another g may do: exp(pi ln(x)) for x^pi, with x only inside it.
        NotInW? failure = null;
        foreach (int gi in Enumerable.Range(0, omega.Count)
            .OrderBy(i => names.Any(name => arguments[i].DependsOn(name.Name)))
            .ThenBy(i => Size(arguments[i])))
        {
            try
            {
                return InW(named, elements, arguments, names, gi);
            }
            catch (NotInW notInW)
            {
                failure ??= notInW;
            }
        }

        throw failure!;
    }

    private (Expr Coefficient, Rational Exponent) InW(Expr named, List<Expr> elements, List<Expr> arguments, List<Variable> names, int gi)
    {
        Expr g = ((Exp)elements[gi]).Argument;
        int sign = SignOf(g);
        Variable w = Fresh();
        var rewritten = new Dictionary<int, Expr>();
        Expr f = named;
        for (int pass = 0; names.FindIndex(name => f.DependsOn(name.Name)) is var i and >= 0; pass++)
        {
            if (pass > 4 * names.Count)
                throw new NotInW($"The elements of {named.Print()} are written in each other.");
            if (!rewritten.TryGetValue(i, out Expr? r))
            {
                Rational c = i == gi ? 1 : Ratio(((Exp)elements[i]).Argument, g);
                r = rewritten[i] = new Exp((arguments[i] - c * arguments[gi]).Simplify()) * new Power(w, sign > 0 ? -c : c);
                if (r.DependsOn(names[i].Name))
                    throw new NotInW($"{elements[i].Print()} is in its own form in {elements[gi].Print()}.");
            }

            f = f.Substitute(names[i].Name, r);
        }

        var expander = new SeriesExpander(w.Name, 0, sign > 0 ? -g : g);
        f = f.Simplify();
        if (expander.Valuation(f) is not { } exponent)
            return (0, 0);

        return (expander.Expand(f, exponent + 1).Leading.Simplify(), exponent);
    }

    private Rational Ratio(Expr a, Expr b)
    {
        Limit ratio = AtInfinity((a / b).Simplify());
        return ratio.Kind == LimitKind.Finite && ratio.Value is Constant { Value: var c } && !c.IsZero
            ? c
            : throw new NotInW($"{a.Print()} and {b.Print()} grow at a ratio that isn't a rational number, {ratio.Print()}.");
    }

    // The most rapidly varying subexpressions: x, or exponentials of something that tends to
    // inf or -inf, of the fastest growth in the comparability class of ln|f|.
    private List<Expr> Mrv(Expr e)
    {
        if (!e.DependsOn(variable))
            return [];

        switch (e)
        {
            case Variable:
                return [e];
            case Exp exp:
                List<Expr> inner = Mrv(exp.Argument);
                return AtInfinity(exp.Argument).Kind == LimitKind.Finite ? inner : Faster([e], inner);
            case Sin or Cos or Tan or Cot or Sec or Csc when AtInfinity(((UnaryExpr)e).Argument).Kind != LimitKind.Finite:
                throw new Oscillation();
        }

        return e.Children.Select(Mrv).Aggregate(new List<Expr>(), Faster);
    }

    private List<Expr> Faster(List<Expr> f, List<Expr> g)
    {
        if (f.Count == 0)
            return g;
        if (g.Count == 0)
            return f;
        if (f.Any(g.Contains))
            return Union(f, g);

        // ln of one over ln of the other: 0 where the first is slower, inf where it is faster.
        Limit ratio = AtInfinity((Logarithm(f[0]) / Logarithm(g[0])).Simplify());
        return ratio.Kind != LimitKind.Finite ? f : IsZero(ratio.Value) ? g : Union(f, g);
    }

    private static List<Expr> Union(List<Expr> f, List<Expr> g) => [.. f, .. g.Where(element => !f.Contains(element))];

    private static Expr Logarithm(Expr e) => e is Exp exp ? exp.Argument : new Ln(e);

    // A product or quotient of more than one exponential with them as one: 2^x/3^x is
    // exp(x ln(2) - x ln(3)), whose rate relative to others is rational where theirs weren't.
    private static Expr? Combined(Expr e)
    {
        List<Expr> top = [], bottom = [];
        Split(e, top, bottom);
        if (top.Count(factor => factor is Exp) + bottom.Count(factor => factor is Exp) < 2)
            return null;

        Expr exponent = top.OfType<Exp>().Select(factor => factor.Argument)
            .Concat(bottom.OfType<Exp>().Select(factor => -factor.Argument))
            .Aggregate((sum, term) => sum + term);
        return Product([new Exp(exponent.Simplify()), .. top.Where(factor => factor is not Exp)]) / Product(bottom.Where(factor => factor is not Exp));
    }

    private static Expr Times(Expr term, Expr factor) => Combined(term * factor) ?? term * factor;

    // The exponent of the fastest exponential term of a sum with more than one: ln of the sum is
    // it plus ln of the sum over that exponential, ln(2^x + 3^x) = x ln(3) + ln((2/3)^x + 1), with
    // one exponential where there were two that grow at an irrational ratio.
    private Expr? Fastest(Expr sum)
    {
        List<Expr> exponents = [.. Terms(sum).Select(Exponent).OfType<Expr>().Where(a => AtInfinity(a).Kind != LimitKind.Finite)];
        return exponents.Count < 2
            ? null
            : exponents.Aggregate((a, b) => AtInfinity((b - a).Simplify()).Kind == LimitKind.PositiveInfinity ? b : a);
    }

    private static Expr? Exponent(Expr term)
    {
        List<Expr> top = [], bottom = [];
        Split(term, top, bottom);
        var exponents = top.OfType<Exp>().Select(factor => factor.Argument).Concat(bottom.OfType<Exp>().Select(factor => -factor.Argument)).ToList();
        return exponents.Count == 0 ? null : exponents.Aggregate((sum, a) => sum + a).Simplify();
    }

    // e with powers of x, the hyperbolic functions and their inverses in exponentials and
    // logarithms, and abs, sign, min, max, floor, ceiling and round, which are eventually
    // smooth, replaced by what they are for large x; atan of something unbounded by pi/2 less
    // atan of its inverse.
    private Expr Prepare(Expr e)
    {
        if (!e.DependsOn(variable))
            return e;

        e = e.MapChildren(Prepare);
        Expr u = e is UnaryExpr unary ? unary.Argument : e;
        return e switch
        {
            Power { Base: EulerNumber } power => new Exp(power.Exponent),
            Power { Base: Exp exp } power => new Exp(power.Exponent * exp.Argument),
            Power power when power.Exponent is not Constant => new Exp(power.Exponent * Prepare(new Ln(power.Base))),
            Sqrt { Argument: Exp exp } => new Exp(exp.Argument / 2),
            Multiply or Divide when Combined(e) is { } combined => combined,
            Ln when u is Add or Subtract && Fastest(u) is { } a => a + new Ln(Terms(u).Select(term => Times(term, new Exp(-a))).Aggregate((sum, term) => sum + term)),
            Sinh => (new Exp(u) - new Exp(-u)) / 2,
            Cosh => (new Exp(u) + new Exp(-u)) / 2,
            Tanh => 1 - 2 / (new Exp(2 * u) + 1),
            Coth => 1 + 2 / (new Exp(2 * u) - 1),
            Sech => 2 / (new Exp(u) + new Exp(-u)),
            Csch => 2 / (new Exp(u) - new Exp(-u)),
            Asinh => new Ln(u + new Sqrt(u * u + 1)),
            Acosh => new Ln(u + new Sqrt(u * u - 1)),
            Atanh => new Ln((1 + u) / (1 - u)) / 2,
            Min min => Prepare((min.Left + min.Right - new Abs(min.Left - min.Right)) / 2),
            Max max => Prepare((max.Left + max.Right + new Abs(max.Left - max.Right)) / 2),
            Abs => SignOf(u) * u,
            Sign => SignOf(u),
            Floor or Ceiling or Round => Step((UnaryExpr)e),
            Atan when AtInfinity(u) is { Kind: not LimitKind.Finite } limit =>
                (limit.Kind == LimitKind.PositiveInfinity ? 1 : -1) * new Pi() / 2 - new Atan(1 / u),
            _ => e,
        };
    }

    // floor, ceiling and round of something that tends to a value: constant next to it, and at
    // a jump the step on the side the argument comes from. Of something unbounded, they are it
    // less a part that oscillates, which Squeezed takes up.
    private Expr Step(UnaryExpr step)
    {
        Limit limit = AtInfinity(step.Argument);
        if (limit.Kind != LimitKind.Finite)
            throw new Oscillation();
        if (limit.Value.GetVariables().Count > 0)
            throw new NotSupportedException($"Whether {step.Print()} jumps where its argument tends to {limit.Print()} isn't known.");

        double value = limit.Value.Evaluate(), shifted = step is Round ? value + 0.5 : value;
        if (shifted != Math.Floor(shifted))
            return new Constant(step.WithChildren([limit.Value]).Evaluate());

        int side = SignOf(step.Argument - limit.Value);
        return side == 0
            ? new Constant(step.WithChildren([limit.Value]).Evaluate())
            : new Constant(step switch
            {
                Floor => side > 0 ? value : value - 1,
                Ceiling => side > 0 ? value + 1 : value,
                _ => side > 0 ? value + 0.5 : value - 0.5,
            });
    }

    // Where a part oscillates: a bounded factor times something that tends to 0, sin(x)/x, a
    // product of factors as large as their unbounded parts, x/(x + sin(x)) as x/x and
    // exp(-x) floor(x) as exp(-x) x, and a sum of terms that have limits.
    private Limit? Squeezed(Expr e)
    {
        if (Unbounded(e) is { } part)
            return AtInfinity(part.Simplify());

        switch (e)
        {
            case Negate negate:
                return Squeezed(negate.Argument)?.Negated();
            case Add or Subtract:
                List<Limit> limits = [.. Terms(e).Select(term => AtInfinity(term.Simplify()))];
                var infinities = limits.Select(limit => limit.Kind).Where(kind => kind != LimitKind.Finite).Distinct().ToList();
                return infinities.Count switch
                {
                    0 => Limit.Finite(SeriesExpander.Tidy(limits.Select(limit => limit.Value).Aggregate((sum, value) => sum + value))),
                    1 => Limit.Infinite(infinities[0] == LimitKind.PositiveInfinity ? 1 : -1),
                    _ => null,
                };
            case Multiply or Divide:
                List<Expr> factors = [], divisors = [];
                Split(e, factors, divisors);
                if (factors.Any(Oscillating))
                {
                    Expr rest = Product(factors.Where(factor => !Oscillating(factor))) / Product(divisors);
                    if (AtInfinity(rest.Simplify()) is { Kind: LimitKind.Finite } limit && IsZero(limit.Value))
                        return Limit.Finite(0);
                }

                if (!factors.Concat(divisors).Any(factor => Unbounded(factor) is not null))
                    return null;

                Expr parts = Product(factors.Select(factor => Unbounded(factor) ?? factor)) / Product(divisors.Select(factor => Unbounded(factor) ?? factor));
                return AtInfinity(parts.Simplify());
        }

        return null;
    }

    // The part a factor is as large as, its ratio to it tending to 1: x for x + sin(x), where
    // it is unbounded and the rest bounded, and for floor(x).
    private Expr? Unbounded(Expr factor)
    {
        switch (factor)
        {
            case Floor or Ceiling or Round:
                Expr u = ((UnaryExpr)factor).Argument;
                return AtInfinity(u).Kind != LimitKind.Finite ? u : null;
            case Add or Subtract:
                List<Expr> terms = [.. Terms(factor)];
                if (!terms.Any(Bounded) || terms.All(Bounded))
                    return null;

                Expr rest = terms.Where(term => !Bounded(term)).Aggregate((sum, term) => sum + term);
                return AtInfinity(rest.Simplify()).Kind != LimitKind.Finite ? rest : null;
            case Power { Exponent: Constant } power when Unbounded(power.Base) is { } part:
                return new Power(part, power.Exponent);
        }

        return null;
    }

    private bool Oscillating(Expr factor) => factor.DependsOn(variable) && Bounded(factor);

    private bool Bounded(Expr e) => !e.DependsOn(variable) || e switch
    {
        Sin or Cos or Atan or Tanh or Sech or Sign => true,
        Add or Subtract or Multiply or Negate => e.Children.All(Bounded),
        Divide quotient => !quotient.Denominator.DependsOn(variable) && Bounded(quotient.Numerator),
        Power { Exponent: Constant { Value: { IsInteger: true, Sign: > 0 } } } power => Bounded(power.Base),
        _ => false,
    };

    private static void Split(Expr e, List<Expr> factors, List<Expr> divisors)
    {
        switch (e)
        {
            case Multiply product:
                Split(product.Left, factors, divisors);
                Split(product.Right, factors, divisors);
                break;
            case Divide quotient:
                Split(quotient.Numerator, factors, divisors);
                Split(quotient.Denominator, divisors, factors);
                break;
            case Negate negate:
                factors.Add(-1);
                Split(negate.Argument, factors, divisors);
                break;
            default:
                factors.Add(e);
                break;
        }
    }

    private static Expr Product(IEnumerable<Expr> factors) => factors.Aggregate((Expr)1, (product, factor) => product * factor);

    private static IEnumerable<Expr> Terms(Expr e) => e switch
    {
        Add sum => Terms(sum.Left).Concat(Terms(sum.Right)),
        Subtract difference => Terms(difference.Left).Concat(Terms(difference.Right).Select(term => -term)),
        _ => [e],
    };

    private static Expr Replace(Expr e, List<Expr> parts, List<Variable> names) =>
        parts.IndexOf(e) is var i and >= 0 ? names[i] : e.MapChildren(child => Replace(child, parts, names));

    private static Expr WithE(Expr e) => e is Exp { Argument: Constant { Value.IsOne: true } } ? new EulerNumber() : e.MapChildren(WithE);

    private static int Size(Expr e) => 1 + e.Children.Sum(Size);

    private static bool IsZero(Expr e) => e.Simplify() is Constant { Value.IsZero: true };

    private Variable Fresh() => new($"{variable}'{++_names}");

    public sealed class UnknownSign(Expr constant) : NotSupportedException($"The sign of {constant.Print()} isn't known.")
    {
        public Expr Constant => constant;
    }

    public sealed class Oscillation() : NotSupportedException("A part oscillates.");

    // The mrv set can't be written in powers of w for the g chosen.
    private sealed class NotInW(string message) : NotSupportedException(message);
}
