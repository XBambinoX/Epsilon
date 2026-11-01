using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed partial class IntegrationTable
{
    // Products and powers of sines and cosines of linear arguments are sums of them by the
    // product-to-sum formulas: exp(x) cos(x)^2 is exp(x)/2 + exp(x) cos(2x)/2, and each term comes
    // back to itself by parts. Where that leaves sinh and cosh in powers or next to another
    // factor, as in cosh(x)^2 cos(x), they are written in exponentials too. Tried after everything
    // else, so that sin(x)^3 stays a polynomial in cos(x).
    private Expr? ByLinearizing(Expr f)
    {
        var factors = new List<Expr>();
        Flatten(f, factors);

        var waves = new Dictionary<(bool Sine, Expr Argument), Expr> { [(false, 0)] = 1 };
        var exponentials = new Dictionary<Expr, Expr> { [0] = 1 };
        Expr rest = 1, others = 1;
        int count = 0, hyperbolic = 0;
        foreach (Expr factor in factors)
        {
            if (Wave(factor) is var (wave, n))
            {
                count += n;
                if (count > MaxMultipleAngle)
                    return null;
                for (int i = 0; i < n; i++)
                    waves = Times(waves, wave);
                continue;
            }

            rest *= factor;
            if (Wave(factor, hyperbolic: true) is var (function, m) && (hyperbolic += m) <= MaxMultipleAngle)
                for (int i = 0; i < m; i++)
                    exponentials = Times(exponentials, function);
            else if (factor is Exp exp && Slope(exp.Argument) is not null)
                exponentials = Times(exponentials, exp);
            else
                others *= factor;
        }

        if (count >= 2 && Integrate(Spread([rest]), expand: false) is { } integral)
            return integral;
        if (count == 0 || hyperbolic == 0 || hyperbolic > MaxMultipleAngle)
            return null;

        return Integrate(Spread(exponentials
            .Where(term => !IsZero(term.Value))
            .Select(term => term.Value * (IsZero(term.Key) ? 1 : new Exp(term.Key)) * others)), expand: false);

        Expr Spread(IEnumerable<Expr> parts) => parts
            .SelectMany(part => waves.Select(term => term.Value * part * (term.Key.Sine ? new Sin(term.Key.Argument) : new Cos(term.Key.Argument))))
            .Aggregate((total, term) => total + term)
            .Simplify();
    }

    private (UnaryExpr Wave, int Power)? Wave(Expr factor, bool hyperbolic = false)
    {
        bool Fits(Expr e) => (hyperbolic ? e is Sinh or Cosh : e is Sin or Cos) && Slope(((UnaryExpr)e).Argument) is not null;
        return factor switch
        {
            UnaryExpr function when Fits(function) => (function, 1),
            Power { Base: UnaryExpr function, Exponent: Constant { Value: { IsInteger: true, Sign: > 0 } n } } when Fits(function) && n <= MaxMultipleAngle =>
                (function, (int)n.Numerator),
            _ => null,
        };
    }

    // cosh u = (exp(u) + exp(-u))/2 and sinh u = (exp(u) - exp(-u))/2, each exponential kept as
    // its argument, so that exp(x) exp(-x) is 1.
    private Dictionary<Expr, Expr> Times(Dictionary<Expr, Expr> exponentials, UnaryExpr function)
    {
        var product = new Dictionary<Expr, Expr>();
        void Add(Expr argument, Expr coefficient)
        {
            argument = Linear(argument);
            product[argument] = product.TryGetValue(argument, out Expr? sum) ? (sum + coefficient).Simplify() : coefficient;
        }

        Expr u = function.Argument;
        foreach (var (a, c) in exponentials)
        {
            if (function is Exp)
            {
                Add(a + u, c);
                continue;
            }

            Expr half = (c / 2).Simplify();
            Add(a + u, half);
            Add(a - u, function is Sinh ? (-half).Simplify() : half);
        }

        return product;
    }

    // sin a sin b = (cos(a - b) - cos(a + b))/2, cos a cos b = (cos(a - b) + cos(a + b))/2 and
    // sin a cos b = (sin(a + b) + sin(a - b))/2, with cos 0 the constant term. The arguments are
    // turned to a positive slope, cos(-u) = cos(u) and sin(-u) = -sin(u), so that terms collect.
    private Dictionary<(bool Sine, Expr Argument), Expr> Times(Dictionary<(bool Sine, Expr Argument), Expr> waves, UnaryExpr wave)
    {
        var product = new Dictionary<(bool Sine, Expr Argument), Expr>();
        void Add(bool sine, Expr argument, Expr coefficient)
        {
            argument = Linear(argument);
            if (Slope(argument) is { } slope && slope.GetVariables().Count == 0 && slope.Evaluate() < 0)
            {
                argument = Linear(-argument);
                coefficient = sine ? (-coefficient).Simplify() : coefficient;
            }

            var key = (sine, argument);
            product[key] = product.TryGetValue(key, out Expr? sum) ? (sum + coefficient).Simplify() : coefficient;
        }

        Expr b = wave.Argument;
        bool sineB = wave is Sin;
        foreach (var ((sineA, a), c) in waves)
        {
            Expr half = (c / 2).Simplify();
            switch (sineA, sineB)
            {
                case (false, false):
                    Add(false, a - b, half);
                    Add(false, a + b, half);
                    break;
                case (true, true):
                    Add(false, a - b, half);
                    Add(false, a + b, (-half).Simplify());
                    break;
                case (true, false):
                    Add(true, a + b, half);
                    Add(true, a - b, half);
                    break;
                default:
                    Add(true, a + b, half);
                    Add(true, b - a, half);
                    break;
            }
        }

        return product;
    }

    // A linear argument as k x + c, so that equal ones are equal: the simplifier leaves -(0 - 2x)
    // as --2x.
    private Expr Linear(Expr argument)
    {
        Expr slope = Derivative(argument)?.Simplify() ?? argument, start = argument.Substitute(variable, 0).Simplify();
        return IsZero(slope) ? start : (slope * _x + start).Simplify();
    }
}
