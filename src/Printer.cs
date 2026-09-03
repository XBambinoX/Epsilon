namespace Epsilon;

public static class Printer
{
    public static string Print(this Expr expr) => PrintInternal(expr, 0);

    private static int Precedence(Expr expr) => expr switch
    {
        Add or Subtract => 1,
        Multiply or Divide => 2,
        Power => 4,
        _ => 5
    };

    private static string PrintInternal(Expr expr, int parentPrecedence)
    {
        int myPrecedence = Precedence(expr);

        string result = expr switch
        {
            Constant c => c.Value.ToString(),
            Variable => "x",

            // Left-associative: left child keeps same precedence (no parens needed),
            // right child needs +1 to force parens on equal precedence (a - (b - c) != a - b - c)
            Add(var l, var r) => $"{PrintInternal(l, myPrecedence)} + {PrintInternal(r, myPrecedence + 1)}",
            Subtract(var l, var r) => $"{PrintInternal(l, myPrecedence)} - {PrintInternal(r, myPrecedence + 1)}",
            Multiply(var l, var r) => $"{PrintInternal(l, myPrecedence)} * {PrintInternal(r, myPrecedence + 1)}",
            Divide(var n, var d) => $"{PrintInternal(n, myPrecedence)} / {PrintInternal(d, myPrecedence + 1)}",

            // Right-associative: reversed — left needs +1, right keeps same
            Power(var b, var e) => $"{PrintInternal(b, myPrecedence + 1)} ^ {PrintInternal(e, myPrecedence)}",

            _ => expr.ToString() ?? string.Empty
        };

        return myPrecedence < parentPrecedence ? $"({result})" : result;
    }
}