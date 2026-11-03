using Epsilon.Core;

namespace Epsilon.Calculus;

/// <summary>Definite integrals computed exactly, from an antiderivative.</summary>
public static class ExactIntegrationExtensions
{
    extension(Expr expr)
    {
        /// <summary>
        /// The exact definite integral by the expression's only variable from <paramref name="a"/>
        /// to <paramref name="b"/>. See <see cref="IntegrateExact(Expr, string, Expr, Expr)"/>.
        /// </summary>
        /// <exception cref="ArgumentException">A limit isn't a real number.</exception>
        /// <exception cref="InvalidOperationException">
        /// The expression has more than one variable, or the integral diverges or doesn't exist,
        /// as for the overload with the variable.
        /// </exception>
        /// <exception cref="NotSupportedException">The integral isn't found.</exception>
        public Expr IntegrateExact(Expr a, Expr b)
        {
            ArgumentNullException.ThrowIfNull(expr);
            return expr.IntegrateExact(expr.GetVariables().Count == 0 ? "x" : expr.GetSingleVariable(), a, b);
        }

        /// <summary>
        /// The definite integral by <paramref name="variable"/> from <paramref name="a"/> to
        /// <paramref name="b"/>, exact: F(b) - F(a) for the antiderivative F that
        /// <c>Integrate(variable)</c> finds, so <c>1/(1 + x^2)</c> from 0 to 1 is pi/4. Where F
        /// jumps between the limits while the integrand doesn't, the jumps are taken off, and where
        /// F is undefined at a limit, its limit there is taken: <c>ln(x)</c> from 0 to 1 is -1.
        /// The integral of an odd integrand over an interval symmetric about 0 is 0 where the
        /// integrand is bounded, with an antiderivative or without. Other variables are constants,
        /// and the limits may have them where F is continuous everywhere.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// A limit depends on <paramref name="variable"/> or isn't a real number.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The integral diverges, as 1/x from -1 to 1 does, or the integrand isn't real on a part
        /// of the interval.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// No antiderivative was found, the limit of the antiderivative at a point where it is
        /// undefined isn't found, or where it is undefined depends on another variable.
        /// </exception>
        public Expr IntegrateExact(string variable, Expr a, Expr b)
        {
            ArgumentNullException.ThrowIfNull(expr);
            ArgumentNullException.ThrowIfNull(variable);
            ArgumentNullException.ThrowIfNull(a);
            ArgumentNullException.ThrowIfNull(b);
            if (a.DependsOn(variable) || b.DependsOn(variable))
                throw new ArgumentException($"A limit of integration depends on {variable}.");

            return new Definite(expr.Simplify(), variable, a.Simplify(), b.Simplify()).Value();
        }
    }

    private sealed class Definite(Expr f, string variable, Expr a, Expr b)
    {
        private const int MaxLogarithmPower = 16;

        private string Integral => $"The integral of {f.Print()} from {a.Print()} to {b.Print()}";

        public Expr Value()
        {
            if ((b - a).Simplify() is Constant { Value.IsZero: true })
                return 0;

            bool numeric = a.GetVariables().Count == 0 && b.GetVariables().Count == 0;
            (Expr Exact, double Value) low = default, high = default;
            int sign = 1;
            if (numeric)
            {
                low = (a, Real(a));
                high = (b, Real(b));
                if (low.Value > high.Value)
                    (low, high, sign) = (high, low, -1);

                if (f.GetVariables().All(name => name == variable) && !ComplexParts.HasImaginaryUnit(f))
                    CheckReal(low.Value, high.Value);
            }

            if ((a + b).Simplify() is Constant { Value.IsZero: true } && Odd() && Bounded(low, high, numeric))
                return 0;

            Expr antiderivative = f.Integrate(variable);
            var parts = Parts(antiderivative);
            var symbols = parts.SelectMany(part => part.GetVariables()).Where(name => name != variable)
                .Concat(numeric || parts.Count == 0 ? [] : a.GetVariables().Concat(b.GetVariables()))
                .Distinct().ToList();
            if (symbols.Count > 0)
                throw new NotSupportedException(
                    $"{Integral} isn't found: whether its antiderivative {antiderivative.Print()} is undefined or jumps between the limits depends on {string.Join(", ", symbols)}.");

            if (!numeric)
                return Tidied(antiderivative.Substitute(variable, b) - antiderivative.Substitute(variable, a));

            Expr sum = End(antiderivative, high, -1) - End(antiderivative, low, 1);
            foreach (IntegrationPoint point in Inside(antiderivative, low.Value, high.Value))
            {
                // At a decimal next to a pole the antiderivative is finite.
                if (point.Approximate)
                    throw new NotSupportedException(FormattableString.Invariant(
                        $"{Integral} isn't found: its antiderivative {antiderivative.Print()} may be undefined at {variable} = {point.Value}, a point that isn't found exactly."));

                Expr left = OneSided(antiderivative, point.Exact, -1), right = OneSided(antiderivative, point.Exact, 1);
                if (Tidied(left - right) is var jump && !Negligible(jump, left))
                    sum += jump;
            }

            return Tidied(sign * sum);
        }

        private List<Expr> Parts(Expr e) =>
            [.. Breakpoints.SingularParts(e, variable, double.NaN, double.NaN).Where(part => part.DependsOn(variable))];

        private static double Real(Expr limit)
        {
            double value;
            try
            {
                value = limit.Evaluate();
            }
            catch (InvalidOperationException)
            {
                value = double.NaN;
            }

            return double.IsFinite(value) ? value : throw new ArgumentException($"The limit of integration {limit.Print()} isn't a real number.");
        }

        // The integrand is real or not all the way between the points where it is undefined.
        private void CheckReal(double low, double high)
        {
            double[] ends = [low, .. Inside(f, low, high).Select(point => point.Value), high];
            for (int i = 0; i + 1 < ends.Length; i++)
            {
                double middle = ends[i] / 2 + ends[i + 1] / 2;
                if (double.IsNaN(f.Evaluate((variable, middle))))
                    throw new InvalidOperationException(FormattableString.Invariant(
                        $"{Integral} doesn't exist: the integrand isn't real at {variable} = {middle}."));
            }
        }

        private bool Odd()
        {
            Expr sum = Reflected(f.Substitute(variable, -new Variable(variable)).Simplify()) + f;
            return sum.Simplify() is Constant { Value.IsZero: true } || sum.Expand().Simplify() is Constant { Value.IsZero: true };
        }

        // Where the integrand has a finite limit from inside the interval at each point it may be
        // undefined at, it is bounded there.
        private bool Bounded((Expr Exact, double Value) low, (Expr Exact, double Value) high, bool numeric)
        {
            var parts = Parts(f);
            if (parts.Count == 0)
                return true;
            if (!numeric || parts.Any(part => part.GetVariables().Any(name => name != variable)))
                return false;

            var inside = Inside(f, low.Value, high.Value);
            if (inside.Any(point => point.Approximate))
                return false;

            var sides = inside.SelectMany(point => new[] { (point.Exact, -1), (point.Exact, 1) }).ToList();
            if (VanishesAt(f, low))
                sides.Add((low.Exact, 1));
            if (VanishesAt(f, high))
                sides.Add((high.Exact, -1));

            try
            {
                return sides.All(side => f.Limit(variable, side.Item1, side.Item2 < 0 ? LimitDirection.Left : LimitDirection.Right).Kind == LimitKind.Finite);
            }
            catch (Exception e) when (e is InvalidOperationException or NotSupportedException)
            {
                return false;
            }
        }

        private Expr End(Expr antiderivative, (Expr Exact, double Value) end, int side)
        {
            if (!VanishesAt(antiderivative, end))
            {
                try
                {
                    return antiderivative.Substitute(variable, end.Exact).Simplify();
                }
                catch (ArithmeticException)
                {
                }
            }

            return OneSided(antiderivative, end.Exact, side);
        }

        private Expr OneSided(Expr antiderivative, Expr point, int side)
        {
            string at = $"{variable} = {point.Print()}", from = side < 0 ? "left" : "right";
            Limit limit;
            try
            {
                limit = antiderivative.Limit(variable, point, side < 0 ? LimitDirection.Left : LimitDirection.Right);
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException($"{Integral} doesn't exist: its antiderivative {antiderivative.Print()} isn't real on the {from} of {at}.", e);
            }
            catch (NotSupportedException e)
            {
                throw new NotSupportedException($"{Integral} isn't found: the limit of its antiderivative {antiderivative.Print()} at {at} from the {from} isn't found.", e);
            }

            return limit.Kind == LimitKind.Finite
                ? limit.Value
                : throw new InvalidOperationException($"{Integral} diverges at {variable} = {point.Print()}.");
        }

        // A root found numerically next to a limit is that limit.
        private List<IntegrationPoint> Inside(Expr e, double low, double high) =>
            [.. Breakpoints.Singular(e, variable, low, high).Where(point => !Near(point.Value, low) && !Near(point.Value, high))];

        private static bool Near(double x, double y) =>
            Math.Abs(x - y) <= 4 * (Math.BitIncrement(Math.Max(Math.Abs(x), Math.Abs(y))) - Math.Max(Math.Abs(x), Math.Abs(y)));

        private bool VanishesAt(Expr e, (Expr Exact, double Value) end)
        {
            foreach (Expr part in Breakpoints.SingularParts(e, variable, end.Value, end.Value).Where(part => part.DependsOn(variable)))
            {
                try
                {
                    if (part.Substitute(variable, end.Exact).Simplify() is var value
                        && (value is Constant { Value.IsZero: true } || !(Math.Abs(value.Evaluate()) > 1e-12)))
                        return true;
                }
                catch (Exception error) when (error is ArithmeticException or InvalidOperationException)
                {
                    return true;
                }
            }

            return false;
        }

        private static Expr Tidied(Expr e)
        {
            Expr tidy = MergedLogarithms(SeriesExpander.Tidy(Reflected(e)));
            return QuadraticSurd.Of(tidy) is { } surd ? surd.ToExpr() : tidy;
        }

        // sin(-u) = -sin(u), cos(-u) = cos(u), (-u)^3 = -u^3, exp(c ln(u)) = u^c and atan(tan(c)) =
        // c - k pi, which the simplifier leaves as they are.
        private static Expr Reflected(Expr e)
        {
            if (e.Children.Length > 0)
                e = e.WithChildren([.. e.Children.Select(Reflected)]);

            return e switch
            {
                Sin or Tan or Cot or Csc or Sinh or Tanh or Coth or Csch or Asin or Atan or Asinh or Atanh or Sign
                    when Opposite(((UnaryExpr)e).Argument) is { } u => -e.WithChildren([u]),
                Cos or Sec or Cosh or Sech or Abs when Opposite(((UnaryExpr)e).Argument) is { } u => e.WithChildren([u]),
                NthRoot { Degree: Constant { Value: { IsInteger: true } n } } root when !n.Numerator.IsEven && Opposite(root.Argument) is { } u =>
                    -new NthRoot(u, root.Degree),
                Power { Exponent: Constant { Value: { IsInteger: true } k } } power when Opposite(power.Base) is { } u =>
                    k.Numerator.IsEven ? new Power(u, power.Exponent) : -new Power(u, power.Exponent),
                Exp { Argument: Multiply { Left: Constant c, Right: Ln ln } } => new Power(ln.Argument, c),
                Exp { Argument: Negate { Argument: Ln ln } } => 1 / ln.Argument,
                Abs abs when SignOf(abs.Argument) is { } sign => sign > 0 ? abs.Argument : -abs.Argument,
                Atan { Argument: Tan tan } when Turns(tan.Argument) is { } k => tan.Argument - k * new Pi(),
                Add or Subtract or Multiply or Divide or Power when e.GetVariables().Count == 0 && QuadraticSurd.Of(e) is { B.IsZero: false } surd => surd.ToExpr(),
                _ => e,
            };
        }

        // The multiple of pi nearest a constant, unless it is halfway between two.
        private static long? Turns(Expr constant)
        {
            if (constant.GetVariables().Count > 0)
                return null;

            try
            {
                double turns = constant.Evaluate() / Math.PI;
                return double.IsFinite(turns) && Math.Abs(Math.Abs(turns - Math.Floor(turns)) - 0.5) > 1e-9 ? (long)Math.Round(turns) : null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static int? SignOf(Expr constant)
        {
            if (constant.GetVariables().Count > 0)
                return null;

            try
            {
                double value = constant.Evaluate();
                return Math.Abs(value) > 1e-12 ? Math.Sign(value) : null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static Expr? Opposite(Expr e) => e switch
        {
            Negate negate => negate.Argument,
            Constant { Value.Sign: < 0 } c => new Constant(-c.Value),
            Multiply { Left: Constant { Value.Sign: < 0 } c } product => new Constant(-c.Value) * product.Right,
            Divide quotient when Opposite(quotient.Numerator) is { } u => u / quotient.Denominator,
            _ => null,
        };

        // a ln(p) + b ln(q) for rationals p, q > 0 and b a multiple of a is a ln(p q^(b/a)):
        // (1/2) ln(3/2) rather than (1/2) ln(1/2) - (1/2) ln(1/3), and -ln(2) rather than ln(1/2).
        private static Expr MergedLogarithms(Expr e)
        {
            var terms = new List<Expr>();
            Terms(e, false, terms);
            var logs = new List<(Rational Coefficient, Rational Argument)>();
            var rest = new List<Expr>();
            foreach (Expr term in terms)
            {
                if (Logarithm(term) is { } log)
                    logs.Add(log);
                else
                    rest.Add(term);
            }

            if (logs.Count == 0)
                return e;

            Rational unit = logs.Select(log => log.Coefficient.Abs()).Min();
            if (logs.Any(log => !(log.Coefficient / unit).IsInteger || (log.Coefficient / unit).Abs() > MaxLogarithmPower))
                return e;

            Rational product = logs.Aggregate(Rational.One, (p, log) => p * log.Argument.Pow((int)(log.Coefficient / unit).Numerator));
            if (product.Numerator.IsOne)
                (unit, product) = (-unit, 1 / product);

            for (int k = MaxLogarithmPower; k >= 2; k--)
                if (Root(product.Numerator, k) is { } top && Root(product.Denominator, k) is { } bottom)
                {
                    (unit, product) = (unit * k, new Rational(top, bottom));
                    break;
                }

            return rest.Aggregate(new Constant(unit) * new Ln(new Constant(product)), (sum, term) => sum + term).Simplify();
        }

        private static System.Numerics.BigInteger? Root(System.Numerics.BigInteger a, int k)
        {
            var r = new System.Numerics.BigInteger(Math.Round(Math.Pow((double)a, 1.0 / k)));
            return System.Numerics.BigInteger.Pow(r, k) == a ? r : null;
        }

        private static void Terms(Expr e, bool negated, List<Expr> terms)
        {
            switch (e)
            {
                case Add sum:
                    Terms(sum.Left, negated, terms);
                    Terms(sum.Right, negated, terms);
                    break;
                case Subtract difference:
                    Terms(difference.Left, negated, terms);
                    Terms(difference.Right, !negated, terms);
                    break;
                case Negate negate:
                    Terms(negate.Argument, !negated, terms);
                    break;
                default:
                    terms.Add(negated ? -e : e);
                    break;
            }
        }

        private static (Rational Coefficient, Rational Argument)? Logarithm(Expr term) => term switch
        {
            Ln { Argument: Constant { Value: { Sign: > 0 } r } } => (Rational.One, r),
            Multiply { Left: Constant c, Right: var log } when Logarithm(log) is var (k, r) => (c.Value * k, r),
            Divide { Numerator: var log, Denominator: Constant { Value.IsZero: false } c } when Logarithm(log) is var (k, r) => (k / c.Value, r),
            Negate negate when Logarithm(negate.Argument) is var (k, r) => (-k, r),
            _ => null,
        };

        private static bool Negligible(Expr jump, Expr scale)
        {
            if (jump is Constant { Value.IsZero: true })
                return true;
            if (jump.GetVariables().Count > 0 || scale.GetVariables().Count > 0)
                return false;

            try
            {
                return Math.Abs(jump.Evaluate()) <= 1e-13 * Math.Max(1, Math.Abs(scale.Evaluate()));
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}
