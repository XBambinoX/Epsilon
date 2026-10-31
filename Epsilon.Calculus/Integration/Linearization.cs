using Epsilon.Core;

namespace Epsilon.Calculus;

internal sealed partial class IntegrationTable
{
    // Products and powers of sines and cosines of linear arguments are sums of them by the
    // product-to-sum formulas: exp(x) cos(x)^2 is exp(x)/2 + exp(x) cos(2x)/2, and each term comes
    // back to itself by parts. Tried after everything else, so that sin(x)^3 stays a polynomial
    // in cos(x).
    private Expr? ByLinearizing(Expr f)
    {
        var factors = new List<Expr>();
        Flatten(f, factors);

        var waves = new Dictionary<(bool Sine, Expr Argument), Expr> { [(false, 0)] = 1 };
        Expr rest = 1;
        int count = 0;
        foreach (Expr factor in factors)
        {
            if (Wave(factor) is not var (wave, n))
            {
                rest *= factor;
                continue;
            }

            count += n;
            if (count > MaxMultipleAngle)
                return null;
            for (int i = 0; i < n; i++)
                waves = Times(waves, wave);
        }

        if (count < 2)
            return null;

        Expr sum = waves.Select(term => term.Value * rest * (term.Key.Sine ? new Sin(term.Key.Argument) : new Cos(term.Key.Argument)))
            .Aggregate((total, term) => total + term)
            .Simplify();
        return Integrate(sum, expand: false);
    }

    private (UnaryExpr Wave, int Power)? Wave(Expr factor) => factor switch
    {
        Sin or Cos when Slope(((UnaryExpr)factor).Argument) is not null => ((UnaryExpr)factor, 1),
        Power { Base: Sin or Cos, Exponent: Constant { Value: { IsInteger: true, Sign: > 0 } n } } power
            when Slope(((UnaryExpr)power.Base).Argument) is not null && n <= MaxMultipleAngle => ((UnaryExpr)power.Base, (int)n.Numerator),
        _ => null,
    };

    // sin a sin b = (cos(a - b) - cos(a + b))/2, cos a cos b = (cos(a - b) + cos(a + b))/2 and
    // sin a cos b = (sin(a + b) + sin(a - b))/2, with cos 0 the constant term. The arguments are
    // turned to a positive slope, cos(-u) = cos(u) and sin(-u) = -sin(u), so that terms collect.
    private Dictionary<(bool Sine, Expr Argument), Expr> Times(Dictionary<(bool Sine, Expr Argument), Expr> waves, UnaryExpr wave)
    {
        var product = new Dictionary<(bool Sine, Expr Argument), Expr>();
        void Add(bool sine, Expr argument, Expr coefficient)
        {
            argument = argument.Simplify();
            if (Slope(argument) is { } slope && slope.GetVariables().Count == 0 && slope.Evaluate() < 0)
            {
                argument = (-argument).Simplify();
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
}
