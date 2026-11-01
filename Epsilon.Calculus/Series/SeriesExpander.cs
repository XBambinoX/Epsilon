using Epsilon.Core;

namespace Epsilon.Calculus;

// The series of an expression in t = x - a, built up from those of its parts: sums term by term,
// products and quotients from the lowest terms of the factors, powers by the binomial series,
// and other functions by their Taylor series at the constant term of the argument.
internal sealed class SeriesExpander(string variable, Expr point)
{
    // How far past a lower bound the first nonzero term of a sum is looked for; beyond it the sum
    // is taken as 0, as sin(x)^2 + cos(x)^2 - 1 is.
    private const int MaxCancellation = 32;

    private readonly Dictionary<(Expr, Rational), Expansion> _known = [];

    private string At => $"{variable} = {point.Print()}";

    public Expansion Expand(Expr e, Rational precision)
    {
        if (!e.DependsOn(variable))
            return IsFinite(e) ? Expansion.Constant(e, precision) : throw new NotSupportedException($"{e.Print()} isn't finite.");
        if (_known.TryGetValue((e, precision), out Expansion? known))
            return known;

        Expr u = e is UnaryExpr unary ? unary.Argument : e;
        Expansion result = e switch
        {
            Variable => Expansion.Of([KeyValuePair.Create(Rational.Zero, point), KeyValuePair.Create(Rational.One, (Expr)1)], precision, false),
            Add sum => Expand(sum.Left, precision).Plus(Expand(sum.Right, precision)),
            Subtract difference => Expand(difference.Left, precision).Plus(Expand(difference.Right, precision).Negated()),
            Negate negate => Expand(negate.Argument, precision).Negated(),
            Multiply product => Product(product.Left, product.Right, precision),
            Divide quotient => Quotient(quotient.Numerator, quotient.Denominator, precision),
            Power power when !power.Exponent.DependsOn(variable) => Raise(power.Base, power.Exponent, precision, root: null),
            Power { Base: EulerNumber } power => Expand(new Exp(power.Exponent), precision),
            Power power => Expand(new Exp(power.Exponent * new Ln(power.Base)), precision),
            Sqrt => Raise(u, new Rational(1, 2), precision, root: null),
            NthRoot root when root.Degree is Constant { Value: { IsInteger: true, Sign: > 0 } n } =>
                Raise(root.Argument, Rational.One / n, precision, root: (int)n.Numerator),
            Tan => Expand(new Sin(u) / new Cos(u), precision),
            Cot => Expand(new Cos(u) / new Sin(u), precision),
            Sec => Expand(1 / new Cos(u), precision),
            Csc => Expand(1 / new Sin(u), precision),
            Tanh => Expand(new Sinh(u) / new Cosh(u), precision),
            Coth => Expand(new Cosh(u) / new Sinh(u), precision),
            Sech => Expand(1 / new Cosh(u), precision),
            Csch => Expand(1 / new Sinh(u), precision),
            Min min => Expand((min.Left + min.Right - new Abs(min.Left - min.Right)) / 2, precision),
            Max max => Expand((max.Left + max.Right + new Abs(max.Left - max.Right)) / 2, precision),
            Abs => Absolute(u, precision, sign: false),
            Sign => Absolute(u, precision, sign: true),
            Floor or Ceiling or Round => Step((UnaryExpr)e, precision),
            Ln => Logarithm((UnaryExpr)e, precision),
            Asin or Acos or Atan or Asinh or Acosh or Atanh => Inverse((UnaryExpr)e, precision),
            UnaryExpr function => Analytic(function, precision),
            _ => throw new NotSupportedException($"{e.Print()} has no series at {At}."),
        };

        _known[(e, precision)] = result;
        return result;
    }

    // The exponent of the first nonzero term, null for 0.
    public Rational? Valuation(Expr e)
    {
        if (!e.DependsOn(variable))
            return e.Simplify() is Constant { Value.IsZero: true } ? null : Rational.Zero;

        switch (e)
        {
            case Variable:
                return point.Simplify() is Constant { Value.IsZero: true } ? Rational.One : Rational.Zero;
            case Negate negate:
                return Valuation(negate.Argument);
            case Multiply product:
                return Valuation(product.Left) is { } left && Valuation(product.Right) is { } right ? left + right : null;
            case Divide quotient:
                return Valuation(quotient.Numerator) is { } numerator ? numerator - Denominator(quotient.Denominator) : null;
            case Power { Exponent: Constant { Value: var k } } power when Valuation(power.Base) is { } v:
                return v * k;
            case Sqrt root when Valuation(root.Argument) is { } v:
                return v / 2;
            case NthRoot { Degree: Constant { Value: var n } } root when Valuation(root.Argument) is { } v:
                return v / n;
        }

        Rational lower = e is Add or Subtract
            ? e.Children.Select(Valuation).OfType<Rational>().DefaultIfEmpty(Rational.Zero).Min()
            : Rational.Zero;
        for (int extra = 1; extra <= MaxCancellation; extra *= 2)
            if (Expand(e, lower + extra).Valuation is { } v)
                return v;

        return null;
    }

    private Rational Denominator(Expr d) =>
        Valuation(d) ?? throw new NotSupportedException($"The denominator {d.Print()} vanishes identically next to {At}.");

    private Expansion Product(Expr left, Expr right, Rational precision)
    {
        if (Valuation(left) is not { } a || Valuation(right) is not { } b || a + b >= precision)
            return Expansion.Zero(precision);

        return Expand(left, precision - b).Times(Expand(right, precision - a)).Truncated(precision);
    }

    // n/d = t^(vn - vd) (n/t^vn)/(d/t^vd): each part to its precision relative to its lowest term.
    private Expansion Quotient(Expr n, Expr d, Rational precision)
    {
        Rational b = Denominator(d);
        if (Valuation(n) is not { } a || a - b >= precision)
            return Expansion.Zero(precision);

        Expansion denominator = Expand(d, precision - a + 2 * b);
        Expansion inverse = Binomial(Relative(denominator, b), -1, precision - a + b).Scaled(1 / denominator.Leading).Shifted(-b);
        return Expand(n, precision + b).Times(inverse).Truncated(precision);
    }

    // e = c t^v (1 + u): the u, of positive valuation.
    private static Expansion Relative(Expansion e, Rational v) => e.Shifted(-v).Scaled(1 / e.Leading).Plus(-1);

    // b^k = c^k t^(v k) (1 + u)^k for b = c t^v (1 + u). Where t^v is raised to a power that
    // isn't an integer, the series holds only for t > 0, as sqrt(x^2) = x does, but where an odd
    // real root gives an integer power: nthroot(x^3, 3) = x.
    private Expansion Raise(Expr b, Expr k, Rational precision, int? root)
    {
        Rational? exponent = k is Constant c ? c.Value : null;
        if (Valuation(b) is not { } v)
            return exponent is { Sign: > 0 } ? Expansion.Zero(precision) : throw new NotSupportedException($"{b.Print()} is 0 next to {At}.");
        if (v != 0 && exponent is null)
            throw new NotSupportedException($"{b.Print()} vanishes at {At} and is raised to {k.Print()}.");

        Rational shift = v == 0 ? 0 : v * exponent!.Value;
        if (shift >= precision)
            return Expansion.Zero(precision);

        Expansion expanded = Expand(b, precision - shift + v);
        Expr lead = expanded.Leading;
        bool integer = exponent is { IsInteger: true }, odd = root is { } n && n % 2 == 1;
        if (!integer && !odd && lead.GetVariables().Count == 0 && !ComplexParts.HasImaginaryUnit(lead) && lead.Evaluate() < 0)
            throw new NotSupportedException($"{b.Print()} is negative next to {At}, where its power {k.Print()} isn't real.");

        Expr scale = Folded((root is { } degree ? new NthRoot(lead, degree) : (Expr)new Power(lead, k)).Simplify());
        bool rightSided = v != 0 && !integer && !(odd && shift.IsInteger);
        return Binomial(Relative(expanded, v), k, precision - shift).Scaled(scale).Shifted(shift).Truncated(precision)
            .OnRight(expanded.RightSided || rightSided);
    }

    // (1 + u)^k = sum of C(k, j) u^j for u of positive valuation.
    private static Expansion Binomial(Expansion u, Expr k, Rational precision)
    {
        Expansion sum = Expansion.Constant(1, precision), power = Expansion.Constant(1, precision);
        if (u.Valuation is not { } step)
            return sum.Truncated(u.Precision).OnRight(u.RightSided);

        Expr c = 1;
        for (int j = 1; j * step < precision; j++)
        {
            c = (c * (k - (j - 1)) / j).Simplify();
            if (c is Constant { Value.IsZero: true })
                break;

            power = power.Times(u).Truncated(precision);
            sum = sum.Plus(power.Scaled(c));
        }

        return sum.Truncated(precision).OnRight(u.RightSided);
    }

    // abs(u) and sign(u) are those of the lowest term: for t > 0 only, unless its power is even.
    private Expansion Absolute(Expr u, Rational precision, bool sign)
    {
        if (Valuation(u) is not { } v)
            return Expansion.Zero(precision);

        Expansion expanded = Expand(u, precision > v ? precision : v + 1);
        Expr lead = expanded.Leading;
        if (lead.GetVariables().Count > 0 || ComplexParts.HasImaginaryUnit(lead))
            throw new NotSupportedException($"The sign of {u.Print()} next to {At} isn't known.");

        int s = Math.Sign(lead.Evaluate());
        bool rightSided = !(v.IsInteger && v.Numerator.IsEven);
        Expansion result = sign ? Expansion.Constant(s, precision) : expanded.Scaled(s).Truncated(precision);
        return result.OnRight(expanded.RightSided || rightSided);
    }

    // floor, ceiling and round are constant next to a point where they don't jump.
    private Expansion Step(UnaryExpr step, Rational precision)
    {
        Expr at = Expand(step.Argument, 1)[0];
        if (at.GetVariables().Count > 0)
            throw new NotSupportedException($"Whether {step.Print()} jumps at {At} isn't known.");

        double value = at.Evaluate(), shifted = step is Round ? value + 0.5 : value;
        if (Valuation(step.Argument - at) is { } v && v <= 0 || shifted == Math.Floor(shifted))
            throw new NotSupportedException($"{step.Print()} jumps at {At}.");

        return Expansion.Constant(step.WithChildren([at]).Evaluate(), precision);
    }

    // ln(c t^v (1 + u)) has ln(t) in it unless v is 0.
    private Expansion Logarithm(UnaryExpr ln, Rational precision)
    {
        if (Valuation(ln.Argument) is not { } v)
            throw new NotSupportedException($"{ln.Print()} is ln(0) next to {At}.");
        if (v != 0)
            throw new NotSupportedException($"{ln.Print()} has a logarithm of {variable} in its series at {At}.");

        return Analytic(ln, precision);
    }

    // f(s) = f(s0) + the integral of f'(s) s' for an inverse function f, whose derivative is
    // algebraic: its own derivatives grow with each step. At a branch point, as acosh at 1, f'
    // has a root and f a Puiseux series.
    private Expansion Inverse(UnaryExpr f, Rational precision)
    {
        if (Valuation(f.Argument) is { } v && v < 0)
            throw new NotSupportedException($"{f.Print()} has no series at {At}, where its argument has a pole.");

        Expr s0 = Expand(f.Argument, 1)[0], value = f.WithChildren([s0]).Simplify();
        if (!IsFinite(value))
            throw new NotSupportedException($"{f.Print()} isn't finite at {At}.");

        Expansion derivative;
        try
        {
            derivative = Expand(f.Differentiate(variable), precision - 1);
        }
        catch (NotSupportedException e)
        {
            throw new NotSupportedException($"{f.Print()} has no series at {At}: {e.Message}", e);
        }

        if (derivative.Terms.ContainsKey(-1))
            throw new NotSupportedException($"{f.Print()} has a logarithm of {variable} in its series at {At}.");

        var integral = derivative.Terms.Select(term => KeyValuePair.Create(term.Key + 1, term.Value / new Constant(term.Key + 1)));
        return Expansion.Of(integral, derivative.Precision + 1, derivative.RightSided).Plus(value).Truncated(precision);
    }

    // f(s0 + h) = sum of f^(j)(s0) h^j / j! for the constant term s0 of the argument.
    private Expansion Analytic(UnaryExpr f, Rational precision)
    {
        if (Valuation(f.Argument) is { } v && v < 0)
            throw new NotSupportedException($"{f.Print()} has an essential singularity at {At}.");
        if (precision <= 0)
            return Expansion.Zero(precision);

        Expansion argument = Expand(f.Argument, precision);
        Expr s0 = argument[0];
        Expansion h = argument.Plus((-s0).Simplify());
        var y = new Variable(FreshName(s0));
        Expr derivative = f.WithChildren([y]);

        Expansion sum = Expansion.Zero(precision), power = Expansion.Constant(1, precision);
        Rational factorial = 1;
        for (int j = 0; ; j++)
        {
            if (j > 0)
                factorial *= j;

            Expr c = (Folded(derivative.Substitute(y.Name, s0).Simplify()) / factorial).Simplify();
            if (!IsFinite(c))
                throw new NotSupportedException($"{f.Print()} isn't analytic at {At}.");

            sum = sum.Plus(power.Scaled(c));
            if (h.Valuation is not { } step || (j + 1) * step >= precision)
                break;

            power = power.Times(h).Truncated(precision);
            derivative = derivative.Differentiate(y.Name).Simplify();
        }

        return sum.Truncated(h.Precision).OnRight(argument.RightSided);
    }

    // A coefficient in the shorter of its simplified and expanded forms, with powers of rationals
    // worked out: (-1/a)^2 as 1/a^2.
    public static Expr Tidy(Expr c)
    {
        Expr folded = Folded(c.Simplify()), expanded = Folded(folded.Expand().Simplify());
        return expanded.Print().Length < folded.Print().Length ? expanded : folded;
    }

    // b^q for a rational b > 0, which the simplifier leaves as 1^(-3/2) and (3/4)^(1/2): rational
    // where b has a rational root, and a + b sqrt(d) for a half-integer q.
    private static Expr Folded(Expr e) => e switch
    {
        Power { Base: Constant { Value: { Sign: > 0 } b }, Exponent: Constant { Value: { IsInteger: false } q } }
            when Root(b.Numerator, q.Denominator) is { } top && Root(b.Denominator, q.Denominator) is { } bottom =>
            new Rational(top, bottom).Pow((int)q.Numerator),
        Power { Base: Constant { Value: { Sign: > 0 } b }, Exponent: Constant { Value: { IsInteger: false } q } }
            when q.Denominator == 2 && QuadraticSurd.Of(new Power(new Sqrt(b), new Constant(new Rational(q.Numerator)))) is { } surd => surd.ToExpr(),
        _ when e.Children.Length > 0 => e.WithChildren([.. e.Children.Select(Folded)]).Simplify(),
        _ => e,
    };

    // The integer n-th root of a nonnegative integer, where there is one.
    private static System.Numerics.BigInteger? Root(System.Numerics.BigInteger a, System.Numerics.BigInteger n)
    {
        if (n > 64)
            return null;

        int degree = (int)n;
        var r = new System.Numerics.BigInteger(Math.Round(Math.Pow((double)a, 1.0 / degree)));
        for (var c = System.Numerics.BigInteger.Max(r - 1, 0); c <= r + 1; c++)
            if (System.Numerics.BigInteger.Pow(c, degree) == a)
                return c;
        return null;
    }

    private string FreshName(Expr e)
    {
        string name = "y";
        while (name == variable || e.DependsOn(name))
            name += "'";
        return name;
    }

    private static bool IsFinite(Expr c)
    {
        if (c.GetVariables().Count > 0)
            return true;
        if (!ComplexParts.HasImaginaryUnit(c))
            return double.IsFinite(c.Evaluate());

        ComplexNumber value = c.EvaluateComplex(new Dictionary<string, ComplexNumber>());
        return double.IsFinite(value.Real) && double.IsFinite(value.Imaginary);
    }
}
