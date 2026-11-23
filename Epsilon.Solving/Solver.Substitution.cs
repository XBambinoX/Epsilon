using Epsilon.Core;

namespace Epsilon.Solving;

// Equations that are polynomials in one part of the unknown, as sin(x)^2 - 3 sin(x) + 2 = 0 in
// sin(x): solved for that part, t^2 - 3t + 2 = 0 at 1 and 2, and then sin(x) = 1 and sin(x) = 2.
internal sealed partial class Solver
{
    // Stands for the part, a name no equation has.
    private const string Part = "#part";

    // The solutions of factor = 0 as a polynomial of degree two or more in one part of the
    // unknown; null where there is no such part, or a solution isn't found completely.
    private List<Solution>? Substituted(Expr factor)
    {
        if (Kernel(factor) is not { } kernel)
            return null;

        Expr Replaced(Expr e) => e.Equals(kernel) ? new Variable(Part) : e.Children.Length == 0 ? e : e.MapChildren(Replaced);
        Expr polynomial = Replaced(factor);
        if (Polynomials.Coefficients(polynomial, Part, MaxDegree) is not { Length: > 2 })
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

        return solutions;
    }

    // The one part of expr that the unknown is in, past sums, products and whole powers: sin(x)
    // in sin(x)^2 - 3 sin(x) + 2. Null where there are several, or the unknown is outside any, as
    // in sin(x) + x.
    private Expr? Kernel(Expr expr)
    {
        Expr? kernel = null;
        bool Walk(Expr e)
        {
            if (!e.DependsOn(variable))
                return true;

            switch (e)
            {
                case Add or Subtract or Multiply or Negate:
                    return e.Children.All(Walk);
                case Divide quotient when !quotient.Denominator.DependsOn(variable):
                    return Walk(quotient.Numerator);
                case Power { Exponent: Constant { Value: { IsInteger: true } n } } power when n.Sign > 0:
                    return Walk(power.Base);
                case Variable:
                    return false;
                default:
                    kernel ??= e;
                    return kernel.Equals(e);
            }
        }

        return Walk(expr) ? kernel : null;
    }
}
