using Epsilon.Core;

namespace Epsilon.Solving;

internal static class Domain
{
    // Where the parts of expr that depend on the variable are defined: denominators that aren't 0,
    // logarithms of positive numbers, even roots of numbers that aren't negative. Over the complex
    // numbers only the denominators.
    internal static List<Condition> Conditions(Expr expr, string variable, bool complex)
    {
        var conditions = new List<Condition>();
        Collect(expr, variable, complex, conditions);
        return [.. conditions.Distinct()];
    }

    private static void Collect(Expr expr, string variable, bool complex, List<Condition> conditions)
    {
        if (!expr.DependsOn(variable))
            return;

        foreach (Expr child in expr.Children)
            Collect(child, variable, complex, conditions);

        switch (expr)
        {
            case Divide quotient:
                Add(quotient.Denominator, Comparison.NotEqual, 0);
                break;
            case Power { Exponent: Constant { Value: var q } } power when q.Sign < 0 && (q.IsInteger || complex):
                Add(power.Base, Comparison.NotEqual, 0);
                break;
            case Power { Exponent: Constant { Value: var q } } power when !q.IsInteger && !complex:
                Add(power.Base, q.Sign < 0 ? Comparison.Greater : Comparison.GreaterOrEqual, 0);
                break;
            case Sqrt root when !complex:
                Add(root.Argument, Comparison.GreaterOrEqual, 0);
                break;
            case NthRoot { Degree: Constant { Value: { IsInteger: true } n } } root when !complex && n.Numerator.IsEven:
                Add(root.Argument, Comparison.GreaterOrEqual, 0);
                break;
            case Ln logarithm:
                Add(logarithm.Argument, complex ? Comparison.NotEqual : Comparison.Greater, 0);
                break;
            case Tan or Sec:
                Add(new Cos(((UnaryExpr)expr).Argument), Comparison.NotEqual, 0);
                break;
            case Cot or Csc:
                Add(new Sin(((UnaryExpr)expr).Argument), Comparison.NotEqual, 0);
                break;
            case Coth or Csch:
                Add(((UnaryExpr)expr).Argument, Comparison.NotEqual, 0);
                break;
            case Asin or Acos when !complex:
                Add(((UnaryExpr)expr).Argument, Comparison.GreaterOrEqual, -1);
                Add(((UnaryExpr)expr).Argument, Comparison.LessOrEqual, 1);
                break;
            case Acosh root when !complex:
                Add(root.Argument, Comparison.GreaterOrEqual, 1);
                break;
            case Atanh root when !complex:
                Add(root.Argument, Comparison.Greater, -1);
                Add(root.Argument, Comparison.Less, 1);
                break;
        }

        void Add(Expr left, Comparison comparison, Expr right) => conditions.Add(new Condition(left, comparison, right));
    }
}
