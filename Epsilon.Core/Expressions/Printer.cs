using System.Globalization;

namespace Epsilon.Core;

public static class Printer
{
    public static string Print(this Expr expr) => PrintInternal(expr, 0);

    private static int Precedence(Expr expr) => expr switch
    {
        Add or Subtract => 1,
        Multiply or Divide => 2,
        Negate => 3,
        Power => 4,
        _ => 5
    };

    private static string PrintInternal(Expr expr, int parentPrecedence)
    {
        int myPrecedence = Precedence(expr);

        string result = expr switch
        {
            Constant c => c.Value.ToString(CultureInfo.InvariantCulture),
            Variable v => v.Name,
            Pi => "π",
            E => "e",
            ImaginaryUnit => "i",
            Negate(var a) => $"-{PrintInternal(a, 2)}",

            Add(var l, Negate(var r)) =>
                $"{PrintInternal(l, myPrecedence)} - {PrintInternal(r, myPrecedence + 1)}",
            Add(var l, Constant r) when r.Value < 0 =>
                $"{PrintInternal(l, myPrecedence)} - {PrintInternal(new Constant(-r.Value), myPrecedence + 1)}",

            Multiply(Constant c, var r) when r is not Constant && c.Value == 1 =>
                PrintInternal(r, parentPrecedence),
            Multiply(Constant c, var r) when r is not Constant && c.Value == -1 =>
                $"-{PrintInternal(r, myPrecedence + 1)}",
            Multiply(Constant c, var r) when r is not Constant =>
                $"{c.Value.ToString(CultureInfo.InvariantCulture)}{PrintInternal(r, myPrecedence + 1)}",

            Add(var l, var r) => $"{PrintInternal(l, myPrecedence)} + {PrintInternal(r, myPrecedence + 1)}",
            Subtract(var l, var r) => $"{PrintInternal(l, myPrecedence)} - {PrintInternal(r, myPrecedence + 1)}",
            Multiply(var l, var r) => $"{PrintInternal(l, myPrecedence)} * {PrintInternal(r, myPrecedence + 1)}",
            Divide(var n, var d) => $"{PrintInternal(n, myPrecedence)} / {PrintInternal(d, myPrecedence + 1)}",

            // Right-associative: reversed — left needs +1, right keeps same
            Power(var b, var e) => $"{PrintInternal(b, myPrecedence + 1)} ^ {PrintInternal(e, myPrecedence)}",

            Sin(var a) => $"sin({PrintInternal(a, 0)})",
            Cos(var a) => $"cos({PrintInternal(a, 0)})",
            Tan(var a) => $"tan({PrintInternal(a, 0)})",
            Cot(var a) => $"cot({PrintInternal(a, 0)})",
            Sec(var a) => $"sec({PrintInternal(a, 0)})",
            Csc(var a) => $"csc({PrintInternal(a, 0)})",
            Asin(var a) => $"asin({PrintInternal(a, 0)})",
            Acos(var a) => $"acos({PrintInternal(a, 0)})",
            Atan(var a) => $"atan({PrintInternal(a, 0)})",
            Sinh(var a) => $"sinh({PrintInternal(a, 0)})",
            Cosh(var a) => $"cosh({PrintInternal(a, 0)})",
            Tanh(var a) => $"tanh({PrintInternal(a, 0)})",
            Asinh(var a) => $"asinh({PrintInternal(a, 0)})",
            Acosh(var a) => $"acosh({PrintInternal(a, 0)})",
            Atanh(var a) => $"atanh({PrintInternal(a, 0)})",
            Coth(var a) => $"coth({PrintInternal(a, 0)})",
            Sech(var a) => $"sech({PrintInternal(a, 0)})",
            Csch(var a) => $"csch({PrintInternal(a, 0)})",
            Exp(var a) => $"exp({PrintInternal(a, 0)})",
            Ln(var a) => $"ln({PrintInternal(a, 0)})",
            Sqrt(var a) => $"sqrt({PrintInternal(a, 0)})",
            NthRoot(var a, var n) => $"nthroot({PrintInternal(a, 0)}, {PrintInternal(n, 0)})",
            Abs(var a) => $"abs({PrintInternal(a, 0)})",
            Sign(var a) => $"sign({PrintInternal(a, 0)})",
            Floor(var a) => $"floor({PrintInternal(a, 0)})",
            Ceiling(var a) => $"ceil({PrintInternal(a, 0)})",
            Round(var a) => $"round({PrintInternal(a, 0)})",
            Min(var l, var r) => $"min({PrintInternal(l, 0)}, {PrintInternal(r, 0)})",
            Max(var l, var r) => $"max({PrintInternal(l, 0)}, {PrintInternal(r, 0)})",

            _ => expr.ToString() ?? string.Empty
        };

        return myPrecedence < parentPrecedence ? $"({result})" : result;
    }
}