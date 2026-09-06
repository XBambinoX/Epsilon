namespace Epsilon.Core;

public static class GradientExtensions
{
    public static IReadOnlyDictionary<string, Expr> Gradient(this Expr expr)
    {
        var result = new Dictionary<string, Expr>();

        foreach (var variable in expr.GetVariables())
            result[variable] = expr.Differentiate(variable);

        return result;
    }
}