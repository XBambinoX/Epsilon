namespace Epsilon.Core;

public static class LatexPrinter
{
    public static string ToLatex(this Expr expr) => LatexInternal(expr, 0);

    private static int Precedence(Expr expr) => expr switch
    {
        Add or Subtract => 1,
        Multiply or Divide => 2,
        Negate => 3,
        Power => 4,
        _ => 5
    };

    private static string LatexInternal(Expr expr, int parentPrecedence)
    {
        int myPrecedence = Precedence(expr);

        string result = expr switch
        {
            Constant c => FormatCoefficient(c.Value),
            Variable v => v.Name,
            Pi => "\\pi",
            E => "e",
            ImaginaryUnit => "i",

            Negate(var a) =>
                $"-{LatexInternal(a, 2)}",

            Add(var l, Negate(var r)) =>
                $"{LatexInternal(l, myPrecedence)} - {LatexInternal(r, myPrecedence + 1)}",

            Add(var l, Constant r) when r.Value < 0 =>
                $"{LatexInternal(l, myPrecedence)} - {LatexInternal(new Constant(-r.Value), myPrecedence + 1)}",

            Add(var l, var r) =>
                $"{LatexInternal(l, myPrecedence)} + {LatexInternal(r, myPrecedence + 1)}",

            Subtract(var l, var r) =>
                $"{LatexInternal(l, myPrecedence)} - {LatexInternal(r, myPrecedence + 1)}",

            Multiply(var l, var r) =>
                LatexMultiply(l, r),
                
            Divide(var n, var d) =>
                $"\\frac{{{LatexInternal(n, 0)}}}{{{LatexInternal(d, 0)}}}",

            Power(var b, var e) =>
                $"{LatexInternal(b, myPrecedence + 1)}^{{{LatexInternal(e, 0)}}}",

            Sin(var a) => $"\\sin\\left({LatexInternal(a, 0)}\\right)",
            Cos(var a) => $"\\cos\\left({LatexInternal(a, 0)}\\right)",
            Tan(var a) => $"\\tan\\left({LatexInternal(a, 0)}\\right)",
            Cot(var a) => $"\\cot\\left({LatexInternal(a, 0)}\\right)",
            Sec(var a) => $"\\sec\\left({LatexInternal(a, 0)}\\right)",
            Csc(var a) => $"\\csc\\left({LatexInternal(a, 0)}\\right)",

            Asin(var a) => $"\\arcsin\\left({LatexInternal(a, 0)}\\right)",
            Acos(var a) => $"\\arccos\\left({LatexInternal(a, 0)}\\right)",
            Atan(var a) => $"\\arctan\\left({LatexInternal(a, 0)}\\right)",

            Sinh(var a) => $"\\sinh\\left({LatexInternal(a, 0)}\\right)",
            Cosh(var a) => $"\\cosh\\left({LatexInternal(a, 0)}\\right)",
            Tanh(var a) => $"\\tanh\\left({LatexInternal(a, 0)}\\right)",

            Asinh(var a) => $"\\operatorname{{asinh}}\\left({LatexInternal(a, 0)}\\right)",
            Acosh(var a) => $"\\operatorname{{acosh}}\\left({LatexInternal(a, 0)}\\right)",
            Atanh(var a) => $"\\operatorname{{atanh}}\\left({LatexInternal(a, 0)}\\right)",

            Coth(var a) => $"\\operatorname{{coth}}\\left({LatexInternal(a, 0)}\\right)",
            Sech(var a) => $"\\operatorname{{sech}}\\left({LatexInternal(a, 0)}\\right)",
            Csch(var a) => $"\\operatorname{{csch}}\\left({LatexInternal(a, 0)}\\right)",

            Exp(var a) => $"e^{{{LatexInternal(a, 0)}}}",
            Ln(var a) => $"\\ln\\left({LatexInternal(a, 0)}\\right)",
            Sqrt(var a) => $"\\sqrt{{{LatexInternal(a, 0)}}}",

            NthRoot(var a, var n) =>
                $"\\sqrt[{LatexInternal(n, 0)}]{{{LatexInternal(a, 0)}}}",

            Abs(var a) => $"\\left|{LatexInternal(a, 0)}\\right|",
            Sign(var a) => $"\\operatorname{{sgn}}\\left({LatexInternal(a, 0)}\\right)",
            Floor(var a) => $"\\left\\lfloor{LatexInternal(a, 0)}\\right\\rfloor",
            Ceiling(var a) => $"\\left\\lceil{LatexInternal(a, 0)}\\right\\rceil",
            Round(var a) => $"\\operatorname{{round}}\\left({LatexInternal(a, 0)}\\right)",

            Min(var l, var r) =>
                $"\\min\\left({LatexInternal(l, 0)}, {LatexInternal(r, 0)}\\right)",

            Max(var l, var r) =>
                $"\\max\\left({LatexInternal(l, 0)}, {LatexInternal(r, 0)}\\right)",

            _ => expr.ToString() ?? string.Empty
        };

        return myPrecedence < parentPrecedence
            ? $"\\left({result}\\right)"
            : result;
    }

    private static string LatexMultiply(Expr left, Expr right)
    {
        var factors = new List<Expr>();
        FlattenMultiply(left, factors);
        FlattenMultiply(right, factors);

        Constant? constant = null;
        int constantCount = 0;
        var rest = new List<Expr>(factors.Count);

        foreach (var factor in factors)
        {
            if (factor is Constant c)
            {
                constant ??= c;
                constantCount++;
            }
            else
            {
                rest.Add(factor);
            }
        }

        bool canUseImplicit = constantCount <= 1 && rest.All(CanBeImplicitFactor);

        if (!canUseImplicit)
            return string.Join(" \\cdot ", factors.Select(f => LatexInternal(f, 3)));

        var coefficient = constant?.Value ?? Rational.One;
        string sign = coefficient.Sign < 0 ? "-" : "";
        var absCoefficient = coefficient.Abs();

        string coefficientPart = absCoefficient.IsOne && rest.Count > 0
            ? ""
            : FormatCoefficient(absCoefficient);

        string restPart = string.Concat(rest.Select(f => LatexInternal(f, Precedence(f))));

        return rest.Count == 0
            ? $"{sign}{FormatCoefficient(absCoefficient)}"
            : $"{sign}{coefficientPart}{restPart}";
    }

    private static void FlattenMultiply(Expr expr, List<Expr> factors)
    {
        if (expr is Multiply(var l, var r))
        {
            FlattenMultiply(l, factors);
            FlattenMultiply(r, factors);
        }
        else
        {
            factors.Add(expr);
        }
    }

    private static bool CanBeImplicitFactor(Expr expr) => expr switch
    {
        Variable => true,

        Sin or Cos or Tan or Cot or Sec or Csc
            or Asin or Acos or Atan
            or Sinh or Cosh or Tanh
            or Asinh or Acosh or Atanh
            or Coth or Sech or Csch
            or Exp or Ln or Sqrt or NthRoot
            or Abs or Sign or Floor or Ceiling or Round
            or Min or Max => true,

        _ => false
    };

    private static string FormatCoefficient(Rational value)
    {
        if (value.IsInteger)
            return value.Numerator.ToString();

        string sign = value.Sign < 0 ? "-" : "";
        var numerator = System.Numerics.BigInteger.Abs(value.Numerator);

        return $"{sign}\\frac{{{numerator}}}{{{value.Denominator}}}";
    }
}