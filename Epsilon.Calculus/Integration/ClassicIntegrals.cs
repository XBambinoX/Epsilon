using Epsilon.Core;

namespace Epsilon.Calculus;

internal static class ClassicIntegrals
{
    private const int MaxPower = 32;

    // Over (-inf, inf): a polynomial times exp of a quadratic, and that exponential times
    // cos or sin of a linear argument.
    public static Expr? Line(Expr f, string variable)
    {
        var (c, factors) = Split(f, variable);
        if (c is null || factors.Count(factor => factor is Exp) != 1)
            return null;

        if (RationalPolynomial.Of(((Exp)factors.First(factor => factor is Exp)).Argument, variable) is not { Degree: 2 } q || q[2].Sign >= 0)
            return null;

        Rational a = -q[2], h = q[1] / (2 * a), e = q[0] + a * h * h;
        Expr root = new Sqrt(new Pi() / new Constant(a));
        var rest = factors.Where(factor => factor is not Exp).ToList();

        // e^(q + i(k x + phi)) over the line is sqrt(pi/a) e^(e - k^2/(4a)) e^(i(k h + phi)).
        if (rest is [Sin or Cos] && Linear(((UnaryExpr)rest[0]).Argument, variable) is var (k, phi))
        {
            Expr angle = k * new Constant(h) + phi;
            return c * root * new Exp(new Constant(e) - k * k / new Constant(4 * a)) * (rest[0] is Cos ? new Cos(angle) : new Sin(angle));
        }

        RationalPolynomial p = RationalPolynomial.One;
        foreach (Expr factor in rest)
        {
            if (RationalPolynomial.Of(factor, variable) is not { } polynomial)
                return null;
            p *= polynomial;
        }

        // x = y + h, and the moments of e^(-a y^2) are (2j - 1)!! / (2a)^j sqrt(pi/a).
        RationalPolynomial shifted = p.Shift(-h);
        Rational sum = 0, moment = 1;
        for (int j = 0; 2 * j <= shifted.Degree; j++)
        {
            sum += shifted[2 * j] * moment;
            moment = moment * (2 * j + 1) / (2 * a);
        }

        return c * new Constant(sum) * root * new Exp(new Constant(e));
    }

    // Over [0, inf): x^s exp(-a x^p) by the gamma function, sin(k x)/x, sin(k x)^2/x^2 and
    // exp(-a x) sin(k x)/x, sin and cos of k x^2, and x^n/(exp(k x) -+ 1) by zeta(n + 1).
    public static Expr? HalfLine(Expr f, string variable)
    {
        var (c, factors) = Split(f, variable);
        return c is null ? Zeta(f, variable) : Gamma(c, factors, variable) ?? Dirichlet(c, factors, variable) ?? Fresnel(c, factors, variable);
    }

    // Over [0, 1]: ln(x)/(1 - x), ln(x)/(1 + x), ln(1 + x)/x and ln(1 - x)/x, by zeta(2).
    public static Expr? Unit(Expr f, string variable)
    {
        if (f is not Divide { Numerator: var numerator } quotient)
            return null;

        var (c, factors) = Split(numerator, variable);
        if (c is null || factors is not [Ln ln]
            || RationalPolynomial.Of(ln.Argument, variable) is not { Degree: 1 } inside
            || RationalPolynomial.Of(quotient.Denominator, variable) is not { Degree: 1 } outside)
            return null;

        // Over m (1 - x) or m (1 + x) with ln(x) above, or over m x with ln(1 + x) or ln(1 - x).
        Rational? value = null, m = null;
        if (inside[0].IsZero && inside[1].IsOne && !outside[0].IsZero)
        {
            m = outside[0];
            value = outside[1] == -m ? new Rational(-1, 6) : outside[1] == m ? new Rational(-1, 12) : null;
        }
        else if (inside[0].IsOne && outside[0].IsZero)
        {
            m = outside[1];
            value = inside[1].IsOne ? new Rational(1, 12) : inside[1] == -1 ? new Rational(-1, 6) : null;
        }

        return value is { } v && m is { } scale ? c * new Constant(v / scale) * new Power(new Pi(), 2) : null;
    }

    // ln(sin(x)) and ln(cos(x)) over [0, pi/2], and ln(sin(x)) over [0, pi].
    public static Expr? LogSine(Expr f, string variable, bool wholeTurn)
    {
        var (c, factors) = Split(f, variable);
        if (c is null || factors is not [Ln { Argument: Sin or Cos } ln] || ((UnaryExpr)ln.Argument).Argument is not Variable { Name: var name } || name != variable
            || wholeTurn && ln.Argument is Cos)
            return null;

        Expr value = -new Pi() * new Ln(2) / 2;
        return c * (wholeTurn ? 2 * value : value);
    }

    // The integral of x^s exp(-a x^p) from 0 to inf is Gamma((s + 1)/p) / (p a^((s + 1)/p)).
    private static Expr? Gamma(Expr c, List<Expr> factors, string variable)
    {
        if (factors.Count(factor => factor is Exp) != 1 || Monomial(((Exp)factors.First(factor => factor is Exp)).Argument, variable) is not var (minusA, p)
            || p.Sign <= 0)
            return null;

        Rational s = 0;
        foreach (Expr factor in factors.Where(factor => factor is not Exp))
        {
            if (Exponent(factor, variable) is not { } power)
                return null;
            s += power;
        }

        Rational z = (s + 1) / p;
        if (z.Sign <= 0 || GammaOf(z) is not { } gamma)
            return null;

        Expr a = (-minusA).Simplify();
        if (!Positive(a))
            return null;

        return c * gamma / (new Constant(p) * new Power(a, new Constant(z)));
    }

    private static Expr? Dirichlet(Expr c, List<Expr> factors, string variable)
    {
        Rational s = 0;
        Expr? sine = null, damping = null;
        int power = 0;
        foreach (Expr factor in factors)
        {
            if (Exponent(factor, variable) is { } exponent)
                s += exponent;
            else if (factor is Sin sin && sine is null)
                (sine, power) = (sin.Argument, 1);
            else if (factor is Power { Base: Sin square, Exponent: Constant { Value: var two } } && two == 2 && sine is null)
                (sine, power) = (square.Argument, 2);
            else if (factor is Exp exp && damping is null)
                damping = exp.Argument;
            else
                return null;
        }

        if (sine is null || s != -power || Linear(sine, variable) is not var (k, phi) || phi is not Constant { Value.IsZero: true })
            return null;

        if (damping is null)
            return power == 1 ? c * SignOf(k) * new Pi() / 2 : c * SignOf(k) * k * new Pi() / 2;

        if (power != 1 || Linear(damping, variable) is not var (minusA, start) || start is not Constant { Value.IsZero: true })
            return null;

        Expr a = (-minusA).Simplify();
        return Positive(a) ? c * new Atan(k / a) : null;
    }

    // The integral of sin(k x^2) and cos(k x^2) from 0 to inf is sqrt(pi/(2|k|))/2.
    private static Expr? Fresnel(Expr c, List<Expr> factors, string variable)
    {
        if (factors is not [Sin or Cos] || Monomial(((UnaryExpr)factors[0]).Argument, variable) is not var (k, p) || p != 2)
            return null;

        Expr value = new Sqrt(new Pi() / (2 * SignOf(k) * k)) / 2;
        return factors[0] is Sin ? c * SignOf(k) * value : c * value;
    }

    // The integral of x^n/(exp(k x) - 1) from 0 to inf is n! zeta(n + 1)/k^(n + 1), and with
    // + 1 that times 1 - 2^-n; zeta(n + 1) is a rational times pi^(n + 1) for an odd n.
    private static Expr? Zeta(Expr f, string variable)
    {
        if (f is not Divide quotient)
            return null;

        var (c, factors) = Split(quotient.Numerator, variable);
        if (c is null || quotient.Denominator is not (Add or Subtract) || quotient.Denominator.Children.FirstOrDefault(child => child is Exp) is not Exp exp
            || (quotient.Denominator - exp).Simplify() is not Constant { Value: var one } || one.Abs() != 1
            || Linear(exp.Argument, variable) is not var (k, start) || start is not Constant { Value.IsZero: true } || !Positive(k))
            return null;

        Rational s = 0;
        foreach (Expr factor in factors)
        {
            if (Exponent(factor, variable) is not { } power)
                return null;
            s += power;
        }

        if (!s.IsInteger || s.Sign <= 0 || s.Numerator.IsEven || s > MaxPower)
            return null;

        int n = (int)s.Numerator, m = (n + 1) / 2;
        Rational zeta = (m % 2 == 1 ? 1 : -1) * Bernoulli(2 * m) * new Rational(System.Numerics.BigInteger.Pow(2, 2 * m)) / (2 * Factorial(2 * m));
        Rational value = Factorial(n) * zeta * (one.Sign < 0 ? 1 : 1 - new Rational(1, System.Numerics.BigInteger.Pow(2, n)));
        return c * new Constant(value) * new Power(new Pi(), n + 1) / new Power(k, n + 1);
    }

    // The constant factor of a product, and the factors with the variable; null where a factor
    // with it is a quotient of its own.
    private static (Expr? Constant, List<Expr> Factors) Split(Expr f, string variable)
    {
        List<Expr> factors = [], divisors = [];
        Gather(f, factors, divisors);
        Expr constant = 1;
        var varying = new List<Expr>();
        foreach (Expr factor in factors)
        {
            if (factor.DependsOn(variable))
                varying.Add(factor);
            else
                constant *= factor;
        }

        foreach (Expr divisor in divisors)
        {
            if (!divisor.DependsOn(variable))
                constant /= divisor;
            else if (Exponent(divisor, variable) is { } power)
                varying.Add(new Power(new Variable(variable), new Constant(-power)));
            else if (divisor is Exp exp)
                varying.Add(new Exp(-exp.Argument));
            else
                return (null, varying);
        }

        return (constant.Simplify(), varying);
    }

    private static void Gather(Expr e, List<Expr> factors, List<Expr> divisors)
    {
        switch (e)
        {
            case Multiply product:
                Gather(product.Left, factors, divisors);
                Gather(product.Right, factors, divisors);
                break;
            case Divide quotient:
                Gather(quotient.Numerator, factors, divisors);
                Gather(quotient.Denominator, divisors, factors);
                break;
            case Negate negate:
                factors.Add(-1);
                Gather(negate.Argument, factors, divisors);
                break;
            default:
                factors.Add(e);
                break;
        }
    }

    // The exponent r of a factor x^r.
    private static Rational? Exponent(Expr factor, string variable) => factor switch
    {
        Variable v when v.Name == variable => 1,
        Power { Base: Variable v, Exponent: Constant { Value: var r } } when v.Name == variable => r,
        Sqrt { Argument: Variable v } when v.Name == variable => new Rational(1, 2),
        NthRoot { Argument: Variable v, Degree: Constant { Value: { IsInteger: true } n } } when v.Name == variable => 1 / n,
        _ => null,
    };

    // c x^p for a constant c.
    private static (Expr Coefficient, Rational Power)? Monomial(Expr u, string variable) => u switch
    {
        Negate negate when Monomial(negate.Argument, variable) is var (c, p) => (-c, p),
        Multiply product when !product.Left.DependsOn(variable) && Monomial(product.Right, variable) is var (c, p) => (product.Left * c, p),
        Multiply product when !product.Right.DependsOn(variable) && Monomial(product.Left, variable) is var (c, p) => (product.Right * c, p),
        Divide quotient when !quotient.Denominator.DependsOn(variable) && Monomial(quotient.Numerator, variable) is var (c, p) => (c / quotient.Denominator, p),
        _ when Exponent(u, variable) is { } p => (1, p),
        _ => null,
    };

    // k x + phi for constants k and phi.
    private static (Expr Slope, Expr Start)? Linear(Expr u, string variable)
    {
        try
        {
            Expr slope = u.Differentiate(variable).Simplify();
            return slope.DependsOn(variable) ? null : (slope, u.Substitute(variable, 0).Simplify());
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool Positive(Expr a) => SignOf(a) > 0;

    // A value that depends on the sign of a symbol throws, and the integral says so.
    private static int SignOf(Expr c)
    {
        Expr simplified = c.Simplify();
        if (simplified.GetVariables().Count > 0)
            throw new Gruntz.UnknownSign(simplified);

        double value = simplified.Evaluate();
        return double.IsFinite(value) ? Math.Sign(value) : throw new Gruntz.UnknownSign(simplified);
    }

    // Gamma(z) for z an integer or an integer and a half: (z - 1)!, and (2n)!/(4^n n!) sqrt(pi) at n + 1/2.
    private static Expr? GammaOf(Rational z)
    {
        if (z.IsInteger)
            return z <= MaxPower ? new Constant(Factorial((int)z.Numerator - 1)) : null;
        if (z.Denominator != 2 || z > MaxPower)
            return null;

        int n = (int)(z - new Rational(1, 2)).Numerator;
        return new Constant(Factorial(2 * n) / (new Rational(System.Numerics.BigInteger.Pow(4, n)) * Factorial(n))) * new Sqrt(new Pi());
    }

    private static Rational Factorial(int n)
    {
        System.Numerics.BigInteger product = 1;
        for (int k = 2; k <= n; k++)
            product *= k;
        return new Rational(product);
    }

    // B_n by the sum over k < n of C(n + 1, k) B_k = -(n + 1) B_n.
    private static Rational Bernoulli(int n)
    {
        var b = new Rational[n + 1];
        b[0] = 1;
        for (int m = 1; m <= n; m++)
        {
            Rational sum = 0;
            System.Numerics.BigInteger binomial = 1;
            for (int k = 0; k < m; k++)
            {
                sum += new Rational(binomial) * b[k];
                binomial = binomial * (m + 1 - k) / (k + 1);
            }

            b[m] = -sum / (m + 1);
        }

        return b[n];
    }
}
