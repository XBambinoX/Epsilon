using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed partial class IntegrationTable(string variable, int depth = 0)
{
    private readonly Variable _x = new(variable);

    public static Expr? Antiderivative(Expr expr, string variable) =>
        new IntegrationTable(variable).Integrate(expr.Simplify())?.Simplify() is { } integral ? FoldNegations(integral) : null;

    private static Expr FoldNegations(Expr e) => e switch
    {
        Negate { Argument: Divide { Numerator: Constant c } quotient } => new Divide(new Constant(-c.Value), FoldNegations(quotient.Denominator)),
        _ when e.Children.Length > 0 => e.WithChildren([.. e.Children.Select(FoldNegations)]),
        _ => e,
    };

    private Expr? Integrate(Expr f, bool expand = true)
    {
        if (!f.DependsOn(variable))
            return f * _x;

        Expr? result = f switch
        {
            Variable => new Power(f, 2) / 2,
            Add add => Integrate(add.Left, expand) is { } left && Integrate(add.Right, expand) is { } right ? left + right : null,
            Subtract difference => Integrate(difference.Left, expand) is { } left && Integrate(difference.Right, expand) is { } right
                ? left - right : null,
            Negate negate => Integrate(negate.Argument, expand) is { } g ? -g : null,
            Multiply product when !product.Left.DependsOn(variable) => Integrate(product.Right, expand) is { } g ? product.Left * g : null,
            Multiply product when !product.Right.DependsOn(variable) => Integrate(product.Left, expand) is { } g ? g * product.Right : null,
            Multiply product => Product(product.Left, product.Right) ?? Product(product.Right, product.Left),
            Divide quotient when !quotient.Denominator.DependsOn(variable) =>
                Integrate(quotient.Numerator, expand) is { } g ? g / quotient.Denominator : null,
            Divide { Denominator: Multiply { Left: var factor } denominator } quotient when !factor.DependsOn(variable) =>
                Integrate(quotient.Numerator / denominator.Right, expand) is { } g ? g / factor : null,
            Divide quotient when !quotient.Numerator.DependsOn(variable) =>
                PowerOf(quotient.Denominator, -1) is { } g ? quotient.Numerator * g : null,
            Divide quotient => Quotient(quotient.Numerator, quotient.Denominator),
            Power power => PowerOf(power.Base, power.Exponent),
            Sqrt root => PowerOf(root.Argument, new Rational(1, 2)),
            NthRoot root => RootPower(root, 1),
            Ln { Argument: Abs abs } when Slope(abs.Argument) is { } a => (abs.Argument * new Ln(abs) - abs.Argument) / a,
            Min min => Integrate((min.Left + min.Right - new Abs(min.Left - min.Right)) / 2, expand),
            Max max => Integrate((max.Left + max.Right + new Abs(max.Left - max.Right)) / 2, expand),
            UnaryExpr function when Slope(function.Argument) is { } a => Elementary(function) is { } g ? Over(g, a) : null,
            _ => null,
        };

        result ??= BySubstitution(f);
        if (result is null && expand && f.Expand() is var expanded && !expanded.Equals(f))
            return Integrate(expanded, expand: false);

        return result;
    }

    private static Expr? Elementary(UnaryExpr function)
    {
        Expr u = function.Argument;
        return function switch
        {
            Exp => new Exp(u),
            Ln => u * new Ln(u) - u,
            Sin => -new Cos(u),
            Cos => new Sin(u),
            Tan => -new Ln(new Abs(new Cos(u))),
            Cot => new Ln(new Abs(new Sin(u))),
            Sec => new Ln(new Abs(new Sec(u) + new Tan(u))),
            Csc => -new Ln(new Abs(new Csc(u) + new Cot(u))),
            Sinh => new Cosh(u),
            Cosh => new Sinh(u),
            Tanh => new Ln(new Cosh(u)),
            Coth => new Ln(new Abs(new Sinh(u))),
            Sech => new Atan(new Sinh(u)),
            Csch => new Ln(new Abs(new Tanh(u / 2))),
            Asin => u * new Asin(u) + new Sqrt(1 - new Power(u, 2)),
            Acos => u * new Acos(u) - new Sqrt(1 - new Power(u, 2)),
            Atan => u * new Atan(u) - new Ln(1 + new Power(u, 2)) / 2,
            Asinh => u * new Asinh(u) - new Sqrt(new Power(u, 2) + 1),
            Acosh => u * new Acosh(u) - new Sqrt(new Power(u, 2) - 1),
            Atanh => u * new Atanh(u) + new Ln(1 - new Power(u, 2)) / 2,
            Abs => u * new Abs(u) / 2,
            Sign => new Abs(u),
            _ => null,
        };
    }

    private Expr? PowerOf(Expr b, Expr e)
    {
        if (!b.DependsOn(variable))
            return Slope(e) is { } a ? new Power(b, e) / ((b is EulerNumber ? 1 : new Ln(b)) * a) : null;
        if (e.DependsOn(variable))
            return null;

        if (Slope(b) is { } slope)
        {
            if (Is(e, -1))
                return new Ln(new Abs(b)) / slope;

            Expr raised = (e + 1).Simplify();
            return raised.Evaluate() < 0
                ? 1 / (raised * slope * new Power(b, (-raised).Simplify()))
                : new Power(b, raised) / (raised * slope);
        }

        return b switch
        {
            Sqrt root => PowerOf(root.Argument, e / 2),
            NthRoot root => RootPower(root, e),
            Exp exp => Integrate(new Exp((exp.Argument * e).Simplify())),
            Power inner when e.Simplify() is Constant { Value.IsInteger: true } && !inner.Exponent.DependsOn(variable) =>
                PowerOf(inner.Base, (inner.Exponent * e).Simplify()),
            UnaryExpr function when e.Simplify() is Constant { Value: var n } && Slope(function.Argument) is { } a =>
                TrigonometricPower(function, n, a),
            _ when QuadraticIntegrals.Of(b, variable) is { } q =>
                Is(e, -1) ? q.Reciprocal()
                : Is(e, new Rational(1, 2)) ? q.Sqrt()
                : Is(e, new Rational(-1, 2)) ? q.InverseSqrt()
                : null,
            _ => null,
        };
    }

    // nthroot(u, n)^m for u linear: n/(n + m) u nthroot(u, n)^m, real wherever the root is,
    // where u^(1 + m/n) would be NaN for u < 0; for m < 0, u nthroot(u, n)^m is nthroot(u, n)^(n + m).
    private Expr? RootPower(NthRoot root, Expr m)
    {
        if (root.Degree is not Constant { Value: { IsInteger: true, Sign: > 0 } } n || Slope(root.Argument) is not { } a)
            return null;

        Expr u = root.Argument;
        if (Is(m + n, 0))
            return new Ln(new Abs(u)) / a;

        Expr g = m.Evaluate() < 0 ? new Power(root, n + m) : u * new Power(root, m);
        return n * g / ((n + m) * a);
    }

    // Squares and reciprocals of trigonometric and hyperbolic functions of a linear u.
    private Expr? TrigonometricPower(UnaryExpr function, Rational n, Expr a)
    {
        if (n == -1 && Reciprocal(function) is { } reciprocal)
            return Integrate(reciprocal);
        if (n == -2 && Reciprocal(function) is { } squared)
            return Integrate(new Power(squared, 2));

        Expr u = function.Argument;
        if (n.IsInteger && n > 2 && function is Tan or Cot or Tanh or Coth)
        {
            Expr square = function switch
            {
                Tan => new Power(new Sec(u), 2) - 1,
                Cot => new Power(new Csc(u), 2) - 1,
                Tanh => 1 - new Power(new Sech(u), 2),
                _ => 1 + new Power(new Csch(u), 2),
            };
            return Integrate((new Power(function, n - 2) * square).Expand());
        }

        if (n != 2)
            return null;
        Expr? g = function switch
        {
            Sin => u / 2 - new Sin(2 * u) / 4,
            Cos => u / 2 + new Sin(2 * u) / 4,
            Tan => new Tan(u) - u,
            Cot => -new Cot(u) - u,
            Sec => new Tan(u),
            Csc => -new Cot(u),
            Sinh => new Sinh(2 * u) / 4 - u / 2,
            Cosh => new Sinh(2 * u) / 4 + u / 2,
            Tanh => u - new Tanh(u),
            Coth => u - new Coth(u),
            Sech => new Tanh(u),
            Csch => -new Coth(u),
            _ => null,
        };

        return g is null ? null : g / a;
    }

    private static Expr? Reciprocal(UnaryExpr function)
    {
        Expr u = function.Argument;
        return function switch
        {
            Sin => new Csc(u),
            Cos => new Sec(u),
            Tan => new Cot(u),
            Cot => new Tan(u),
            Sec => new Cos(u),
            Csc => new Sin(u),
            Sinh => new Csch(u),
            Cosh => new Sech(u),
            Tanh => new Coth(u),
            Coth => new Tanh(u),
            Sech => new Cosh(u),
            Csch => new Sinh(u),
            Exp => new Exp(-u),
            _ => null,
        };
    }

    // Products of functions of linear arguments: sec u tan u and its kin, and sin and cos by
    // the product-to-sum formulas.
    private Expr? Product(Expr left, Expr right)
    {
        static (Expr Base, Expr Exponent) Split(Expr e) => e switch
        {
            Power power => (power.Base, power.Exponent),
            Sqrt root => (root.Argument, new Rational(1, 2)),
            _ => (e, 1),
        };

        var (leftBase, leftExponent) = Split(left);
        var (rightBase, rightExponent) = Split(right);
        if (leftBase.Equals(rightBase) && !leftExponent.DependsOn(variable) && !rightExponent.DependsOn(variable))
            return PowerOf(leftBase, (leftExponent + rightExponent).Simplify());

        if (left is not UnaryExpr f || right is not UnaryExpr g || Slope(f.Argument) is not { } a || Slope(g.Argument) is null)
            return null;

        Expr u = f.Argument, v = g.Argument;
        Expr sum = (u + v).Simplify(), difference = (u - v).Simplify();
        bool same = u.Equals(v);

        return (f, g) switch
        {
            (Sec, Tan) when same => new Sec(u) / a,
            (Csc, Cot) when same => -new Csc(u) / a,
            (Sech, Tanh) when same => -new Sech(u) / a,
            (Csch, Coth) when same => -new Csch(u) / a,
            (Sin, Cos) => Integrate((new Sin(sum) + new Sin(difference)) / 2),
            (Sin, Sin) => Integrate((new Cos(difference) - new Cos(sum)) / 2),
            (Cos, Cos) => Integrate((new Cos(difference) + new Cos(sum)) / 2),
            _ => null,
        };
    }

    // A polynomial over one of no higher degree is divided out first; then linear over a
    // quadratic or over the square root of one.
    private Expr? Quotient(Expr numerator, Expr denominator)
    {
        if (Coefficients(numerator) is { } n && Coefficients(denominator) is { Length: > 1 } d && n.Length >= d.Length)
        {
            var (quotient, remainder) = DividePolynomials(n, d);
            if (Integrate(Polynomial(quotient)) is not { } whole)
                return null;
            if (remainder.All(c => c is Constant { Value.IsZero: true }))
                return whole;

            return Integrate(Polynomial(remainder) / denominator) is { } rest ? whole + rest : null;
        }

        if (Slope(numerator) is not { } alpha)
            return null;

        Expr beta = numerator.Substitute(variable, 0).Simplify();
        if (QuadraticIntegrals.Of(denominator, variable) is { } q)
            return q.LinearOver(alpha, beta);

        Expr? radicand = denominator switch
        {
            Sqrt root => root.Argument,
            Power power when Is(power.Exponent, new Rational(1, 2)) => power.Base,
            _ => null,
        };

        return radicand is not null && QuadraticIntegrals.Of(radicand, variable) is { } r ? r.LinearOverSqrt(alpha, beta) : null;
    }

    // The coefficients of a polynomial in the variable, lowest degree first, as its derivatives
    // at 0 over k!; null for anything that isn't built as a polynomial.
    private Expr[]? Coefficients(Expr polynomial)
    {
        if (!IsPolynomial(polynomial))
            return null;

        var coefficients = new List<Expr>();
        Expr derivative = polynomial;
        for (int k = 0; ; k++)
        {
            coefficients.Add((derivative.Substitute(variable, 0) / Factorial(k)).Simplify());
            if (!derivative.DependsOn(variable))
                break;
            derivative = derivative.Differentiate(variable);
        }

        while (coefficients.Count > 1 && coefficients[^1] is Constant { Value.IsZero: true })
            coefficients.RemoveAt(coefficients.Count - 1);

        return [.. coefficients];
    }

    private bool IsPolynomial(Expr e) => !e.DependsOn(variable) || e switch
    {
        Variable => true,
        Add or Subtract or Multiply or Negate => e.Children.All(IsPolynomial),
        Divide quotient => !quotient.Denominator.DependsOn(variable) && IsPolynomial(quotient.Numerator),
        Power power => power.Exponent is Constant { Value: { IsInteger: true, Sign: >= 0 } } && IsPolynomial(power.Base),
        _ => false,
    };

    private static Expr Factorial(int k) => Enumerable.Range(1, k).Aggregate(Rational.One, (product, i) => product * i);

    // Long division of coefficient lists: n = quotient d + remainder, the remainder of lower degree than d.
    private static (Expr[] Quotient, Expr[] Remainder) DividePolynomials(Expr[] n, Expr[] d)
    {
        int degree = d.Length - 1;
        var remainder = (Expr[])n.Clone();
        var quotient = new Expr[n.Length - degree];
        for (int k = quotient.Length - 1; k >= 0; k--)
        {
            quotient[k] = (remainder[k + degree] / d[degree]).Simplify();
            for (int j = 0; j <= degree; j++)
                remainder[k + j] = (remainder[k + j] - quotient[k] * d[j]).Simplify();
        }

        return (quotient, remainder[..degree]);
    }

    private Expr Polynomial(Expr[] coefficients) =>
        coefficients.Select((c, k) => c * new Power(_x, k)).Aggregate((sum, term) => sum + term).Simplify();

    // u' when u is linear in the variable: a derivative that is a constant other than 0.
    private Expr? Slope(Expr u)
    {
        if (!u.DependsOn(variable) || Derivative(u) is not { } slope || slope.DependsOn(variable) || slope is Constant { Value.IsZero: true })
            return null;

        return slope.GetVariables().Count > 0 || slope.Evaluate() != 0 ? slope : null;
    }

    private Expr? Derivative(Expr u)
    {
        try
        {
            return u.Differentiate(variable);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool Is(Expr e, Rational value) => e.Simplify() is Constant c && c.Value == value;

    // Divided term by term, which Simplify doesn't do: (u acos(u) - sqrt(1 - u^2)) / (1/2) for u = x/2.
    private static Expr Over(Expr g, Expr a) => g switch
    {
        Add sum => Over(sum.Left, a) + Over(sum.Right, a),
        Subtract difference => Over(difference.Left, a) - Over(difference.Right, a),
        _ => (g / a).Simplify(),
    };
}
