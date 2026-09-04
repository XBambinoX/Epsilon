namespace Epsilon.Core;

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
            Pi => "π",
            E => "e",

            // Left-associative: left child keeps same precedence (no parens needed),
            // right child needs +1 to force parens on equal precedence (a - (b - c) != a - b - c)
            Add(var l, var r) => $"{PrintInternal(l, myPrecedence)} + {PrintInternal(r, myPrecedence + 1)}",
            Subtract(var l, var r) => $"{PrintInternal(l, myPrecedence)} - {PrintInternal(r, myPrecedence + 1)}",
            Multiply(var l, var r) => $"{PrintInternal(l, myPrecedence)} * {PrintInternal(r, myPrecedence + 1)}",
            Divide(var n, var d) => $"{PrintInternal(n, myPrecedence)} / {PrintInternal(d, myPrecedence + 1)}",

            // Right-associative: reversed — left needs +1, right keeps same
            Power(var b, var e) => $"{PrintInternal(b, myPrecedence + 1)} ^ {PrintInternal(e, myPrecedence)}",

            Cot(var a) => $"cot({PrintInternal(a, 0)})",
            Sec(var a) => $"sec({PrintInternal(a, 0)})",
            Csc(var a) => $"csc({PrintInternal(a, 0)})",
            Asin(var a) => $"asin({PrintInternal(a, 0)})",
            Acos(var a) => $"acos({PrintInternal(a, 0)})",
            Atan(var a) => $"atan({PrintInternal(a, 0)})",
            Sinh(var a) => $"sinh({PrintInternal(a, 0)})",
            Cosh(var a) => $"cosh({PrintInternal(a, 0)})",
            Tanh(var a) => $"tanh({PrintInternal(a, 0)})",
            Exp(var a) => $"exp({PrintInternal(a, 0)})",
            Ln(var a) => $"ln({PrintInternal(a, 0)})",
            Sqrt(var a) => $"sqrt({PrintInternal(a, 0)})",
            NthRoot(var a, var n) => $"nthroot({PrintInternal(a, 0)}, {PrintInternal(n, 0)})",
            _ => expr.ToString() ?? string.Empty
        };

        return myPrecedence < parentPrecedence ? $"({result})" : result;
    }
}