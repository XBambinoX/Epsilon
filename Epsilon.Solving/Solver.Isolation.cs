using Epsilon.Core;

namespace Epsilon.Solving;

internal sealed partial class Solver
{
    private readonly record struct Step(Expr Inner, Expr Value, Condition[] Needs, bool Inverts);

    private readonly record struct Branch(Expr Inner, Expr Value, List<Condition> Given);

    // factor = 0 by undoing what is done to the unknown, outermost first: 3 ln(x - 1) - 3 = 0
    // gives ln(x - 1) = 1, then x - 1 = e, which is solved as an equation of its own. Each step
    // needs something of the other side: exp(u) = c has a solution only if c > 0. Null where no
    // function can be undone, as in x - cos(x), or the rest isn't solved completely.
    private List<Solution>? Isolated(Expr factor)
    {
        if (Undone(factor, 0, [], inverted: false) is not { } branches)
            return null;

        var solutions = new List<Solution>();
        foreach (var (inner, value, given) in branches)
        {
            SolutionSet rest = new Solver(variable, assumptions, complex, numeric, region, this).Solve(inner, value);
            if (!rest.IsComplete || rest.IsEveryValue)
                return null;
            solutions.AddRange(rest.Solutions.Select(solution => new Solution(solution.Value, [.. solution.Conditions, .. given])));
        }

        return solutions;
    }

    // The equations lhs = rhs comes to once everything that can be is undone, down to a
    // polynomial, with what each needs; null if no function was undone on the way: x + sin(x) = 1
    // would only come back as itself.
    private List<Branch>? Undone(Expr lhs, Expr rhs, List<Condition> given, bool inverted)
    {
        if (inverted && Polynomials.Coefficients(lhs, variable, MaxDegree) is not null || StepsOf(lhs, rhs) is not { } steps)
            return inverted ? [new Branch(lhs, rhs, given)] : null;

        var branches = new List<Branch>();
        foreach (var (inner, value, needs, inverts) in steps)
        {
            var conditions = new List<Condition>(given);
            if (needs.Select(Decide).Any(holds => holds == false))
                continue;
            conditions.AddRange(needs.Where(need => Decide(need) is null).Select(Tidied));

            if (Undone(inner, value.Simplify(assumptions), conditions, inverted || inverts) is not { } more)
                return null;
            branches.AddRange(more);
        }

        return branches;
    }

    // One layer of lhs = rhs undone, or null if the outermost operation can't be: the unknown
    // is on both sides of it, or it has no inverse here.
    private Step[]? StepsOf(Expr lhs, Expr rhs)
    {
        bool Free(Expr e) => !e.DependsOn(variable);
        static Condition Is(Expr left, Comparison comparison, Expr right) => new(left, comparison, right);
        Expr halfPi = Expr.PI / 2;

        switch (lhs)
        {
            case Add sum when Free(sum.Right):
                return [new(sum.Left, rhs - sum.Right, [], false)];
            case Add sum when Free(sum.Left):
                return [new(sum.Right, rhs - sum.Left, [], false)];
            case Subtract difference when Free(difference.Right):
                return [new(difference.Left, rhs + difference.Right, [], false)];
            case Subtract difference when Free(difference.Left):
                return [new(difference.Right, difference.Left - rhs, [], false)];
            case Negate negation:
                return [new(negation.Argument, -rhs, [], false)];
            case Multiply product when Free(product.Left):
                return [new(product.Right, rhs / product.Left, [Is(product.Left, Comparison.NotEqual, 0)], false)];
            case Multiply product when Free(product.Right):
                return [new(product.Left, rhs / product.Right, [Is(product.Right, Comparison.NotEqual, 0)], false)];
            case Divide quotient when Free(quotient.Denominator):
                return [new(quotient.Numerator, rhs * quotient.Denominator, [Is(quotient.Denominator, Comparison.NotEqual, 0)], false)];
            case Divide quotient when Free(quotient.Numerator):
                return [new(quotient.Denominator, quotient.Numerator / rhs, [Is(rhs, Comparison.NotEqual, 0)], true)];

            // u^n = c: an odd n has one real root of c, an even n two, if c >= 0.
            case Power { Exponent: Constant { Value: { IsInteger: true } n } } power when n.Sign > 0:
                Expr root = n == 2 ? new Sqrt(rhs) : new NthRoot(rhs, (int)n.Numerator);
                return n.Numerator.IsEven
                    ? [new(power.Base, root, [Is(rhs, Comparison.GreaterOrEqual, 0)], true), new(power.Base, -root, [Is(rhs, Comparison.GreaterOrEqual, 0)], true)]
                    : [new(power.Base, root, [], true)];
            case Power power when Free(power.Base):
                return [new(power.Exponent, new Ln(rhs) / new Ln(power.Base),
                    [Is(rhs, Comparison.Greater, 0), Is(power.Base, Comparison.Greater, 0), Is(power.Base, Comparison.NotEqual, 1)], true)];

            case Exp e:
                return [new(e.Argument, new Ln(rhs), [Is(rhs, Comparison.Greater, 0)], true)];
            case Ln l:
                return [new(l.Argument, rhs is Constant { Value.IsOne: true } ? Expr.E : new Exp(rhs), [], true)];
            case Sqrt r:
                return [new(r.Argument, new Power(rhs, 2), [Is(rhs, Comparison.GreaterOrEqual, 0)], true)];
            case NthRoot { Degree: Constant { Value: { IsInteger: true } n } } r when n.Sign > 0:
                return [new(r.Argument, new Power(rhs, n), n.Numerator.IsEven ? [Is(rhs, Comparison.GreaterOrEqual, 0)] : [], true)];
            case Abs a:
                return [new(a.Argument, rhs, [Is(rhs, Comparison.GreaterOrEqual, 0)], true), new(a.Argument, -rhs, [Is(rhs, Comparison.GreaterOrEqual, 0)], true)];

            case Asin f:
                return [new(f.Argument, new Sin(rhs), [Is(rhs, Comparison.GreaterOrEqual, -halfPi), Is(rhs, Comparison.LessOrEqual, halfPi)], true)];
            case Acos f:
                return [new(f.Argument, new Cos(rhs), [Is(rhs, Comparison.GreaterOrEqual, 0), Is(rhs, Comparison.LessOrEqual, Expr.PI)], true)];
            case Atan f:
                return [new(f.Argument, new Tan(rhs), [Is(rhs, Comparison.Greater, -halfPi), Is(rhs, Comparison.Less, halfPi)], true)];

            case Sinh f:
                return [new(f.Argument, new Asinh(rhs), [], true)];
            case Cosh f:
                return [new(f.Argument, new Acosh(rhs), [Is(rhs, Comparison.GreaterOrEqual, 1)], true), new(f.Argument, -new Acosh(rhs), [Is(rhs, Comparison.GreaterOrEqual, 1)], true)];
            case Tanh f:
                return [new(f.Argument, new Atanh(rhs), [Is(rhs, Comparison.Greater, -1), Is(rhs, Comparison.Less, 1)], true)];
            case Coth f:
                return [new(f.Argument, new Atanh(1 / rhs), [Is(new Abs(rhs), Comparison.Greater, 1)], true)];
            case Sech f:
                Condition[] sech = [Is(rhs, Comparison.Greater, 0), Is(rhs, Comparison.LessOrEqual, 1)];
                return [new(f.Argument, new Acosh(1 / rhs), sech, true), new(f.Argument, -new Acosh(1 / rhs), sech, true)];
            case Csch f:
                return [new(f.Argument, new Asinh(1 / rhs), [Is(rhs, Comparison.NotEqual, 0)], true)];
            case Asinh f:
                return [new(f.Argument, new Sinh(rhs), [], true)];
            case Acosh f:
                return [new(f.Argument, new Cosh(rhs), [Is(rhs, Comparison.GreaterOrEqual, 0)], true)];
            case Atanh f:
                return [new(f.Argument, new Tanh(rhs), [], true)];

            default:
                return null;
        }
    }
}
