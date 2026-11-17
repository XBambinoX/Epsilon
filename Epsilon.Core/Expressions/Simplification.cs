namespace Epsilon.Core;

/// <summary>Algebraic simplification of expressions.</summary>
public static class Simplifier
{
    /// <summary>
    /// Simplifies without any assumptions about the variables: folds constants, combines like terms
    /// and factors (<c>2x + 3x = 5x</c>, <c>x*x = x^2</c>), cancels common factors and applies
    /// identities such as <c>sin(x)^2 + cos(x)^2 = 1</c>.
    /// </summary>
    /// <param name="expr">The expression to simplify.</param>
    /// <param name="mode">
    /// Whether rewrites may enlarge the domain (<see cref="SimplifyMode.Generic"/>, the default:
    /// <c>x/x = 1</c>) or must keep it exactly (<see cref="SimplifyMode.Strict"/>).
    /// </param>
    /// <returns>An equivalent expression in canonical form.</returns>
    public static Expr Simplify(this Expr expr, SimplifyMode mode = SimplifyMode.Generic) =>
        expr.Simplify(Assumptions.None, mode);

    /// <summary>
    /// Simplifies using what is known about the variables, which unlocks rules that are only valid
    /// under conditions: with x &gt; 0, <c>sqrt(x^2) = x</c> instead of <c>|x|</c>.
    /// </summary>
    /// <param name="expr">The expression to simplify.</param>
    /// <param name="assumptions">What is known about the variables.</param>
    /// <param name="mode">See <see cref="Simplify(Expr, SimplifyMode)"/>.</param>
    /// <returns>An equivalent expression in canonical form.</returns>
    public static Expr Simplify(this Expr expr, Assumptions assumptions, SimplifyMode mode = SimplifyMode.Generic) =>
        SimplifyCached(expr, assumptions, mode, new Dictionary<Expr, Expr>());

    // One call simplifies the same subtrees over and over: every fixpoint step re-simplifies
    // all children
    private static Expr SimplifyCached(Expr expr, Assumptions assumptions, SimplifyMode mode, Dictionary<Expr, Expr> cache)
    {
        if (cache.TryGetValue(expr, out Expr? cached))
            return cached;

        Expr result = IterateToFixpoint(expr.Canonicalize(), e => SimplifyOnce(e, assumptions, mode, cache));
        cache[expr] = result;
        cache.TryAdd(result, result);
        return result;
    }

    private const int MaxSimplifyIterations = 100;
    private const int UntrackedIterations = 8;

    // Applies `step` until the expression stops changing. Every step is an equivalent rewrite,
    // so if two rules undo each other (a cycle) or the steps never settle, any expression seen
    // so far is a correct answer: the smallest one is returned instead of throwing at the user.
    // A repeated expression ends the loop at once, and the choice doesn't depend on where in
    // the cycle it was noticed.
    internal static Expr IterateToFixpoint(Expr start, Func<Expr, Expr> step, int maxIterations = MaxSimplifyIterations)
    {
        // Fast path: almost every call settles within a step or two. Simplify runs this for
        // every subtree, so the history below is only built for the rare slow cases.
        Expr current = start;
        int untracked = Math.Min(UntrackedIterations, maxIterations);
        for (int i = 0; i < untracked; i++)
        {
            Expr next = step(current);
            if (next.Equals(current))
                return next;
            current = next;
        }

        // Walks on until an expression repeats, so a cycle is always seen in full, whichever
        // of its expressions the fast path stopped at.
        var seen = new List<Expr> { current };
        var seenSet = new HashSet<Expr> { current };

        for (int i = untracked; i < maxIterations; i++)
        {
            Expr next = step(current);

            if (next.Equals(current))
                return next;

            if (!seenSet.Add(next))
                break;

            seen.Add(next);
            current = next;
        }

        // The start is a candidate too: if the steps only ever grow it, it's the best answer.
        // Ties are broken by the printed form, not by order, which depends on the entry point.
        return seen.Append(start)
            .OrderBy(NodeCount)
            .ThenBy(e => e.ToString(), StringComparer.Ordinal)
            .First();
    }

    private static int NodeCount(Expr expr)
    {
        int count = 1;
        foreach (Expr child in expr.Children)
            count += NodeCount(child);
        return count;
    }

    private static Expr SimplifyOnce(Expr expr, Assumptions assumptions, SimplifyMode mode, Dictionary<Expr, Expr> cache)
    {
        if (expr is Add or Subtract)
            return SimplifySum(expr, assumptions, mode, cache).Canonicalize();

        Expr simplifiedChildren = expr.MapChildren(child => SimplifyCached(child, assumptions, mode, cache));
        return ApplyRules(simplifiedChildren, assumptions, mode, cache).Canonicalize();
    }

    // Domain guards. A rewrite that only enlarges the domain (x/x -> 1 gains x = 0) is
    // allowed in Generic mode, but in Strict mode needs a proof from the assumptions.
    // Generic mode still rejects a literal 0, so 0/0 and similar are never folded.
    private static bool NonZeroForDomain(Expr expr, Assumptions assumptions, SimplifyMode mode) =>
        mode == SimplifyMode.Generic
            ? !(expr is Constant c && c.Value.IsZero)
            : expr.IsProvablyNonZero(assumptions);

    private static bool NonNegativeForDomain(Expr expr, Assumptions assumptions, SimplifyMode mode) =>
        mode == SimplifyMode.Generic
            ? !expr.IsProvablyNegative(assumptions)
            : expr.IsProvablyNonNegative(assumptions);

    private static bool PositiveForDomain(Expr expr, Assumptions assumptions, SimplifyMode mode) =>
        mode == SimplifyMode.Generic
            ? !expr.IsProvablyNonPositive(assumptions)
            : expr.IsProvablyPositive(assumptions);

    // Sums never get here: SimplifySum handles a whole Add/Subtract chain at once.
    private static Expr ApplyRules(Expr expr, Assumptions assumptions, SimplifyMode mode, Dictionary<Expr, Expr> cache)
    {
        Expr combinedProduct = CombineProductFactors(expr, assumptions, mode);
        if (!combinedProduct.Equals(expr))
            return combinedProduct.Canonicalize();

        switch (expr)
        {
            case Negate(Constant c):
                return new Constant(-c.Value);

            case Negate(Negate(var a)):
                return a;

            // -(-2/y) = 2/y, -(-y/2) = y/2: the minus cancels the sign of the numerator.
            case Negate(Divide(var n, var d)) when ConstantFactor(n).Sign < 0:
                return new Divide(new Negate(n), d);

            // Constant folding, 1 * x and equal-base merging for products are all done by
            // CombineProductFactors above; only 0 * x is deliberately left to this rule.
            case Multiply(var l, var r) when l.Equals(new Constant(0)) || r.Equals(new Constant(0)):
                return new Constant(0);

            case Divide(var numerator, var denominator)
                when TryCancelCommonFactors(numerator, denominator, assumptions, mode, cache, out Expr? cancelled):
                return cancelled!;

            case Divide(var numerator, var denominator)
                when TryCancelPolynomialFactors(numerator, denominator, assumptions, mode, out Expr? divided):
                return divided!;

            case Divide(Constant a, Constant b) when !b.Value.IsZero:
                return new Constant(a.Value / b.Value);

            // 1/(1 + 1/x) = x/(x + 1), 1/((y^2/z^2 + 1) z) = z/(y^2 + z^2): a sum with fractions in
            // a denominator goes over its common denominator L, which moves up. The quotient wasn't
            // defined where L = 0, so Strict mode needs L provably nonzero.
            case Divide(var n, var d) when TryClearFractionsBelow(n, d, assumptions, mode, out Expr? cleared):
                return cleared!;

            // 0 / d = 0 - enlarges the domain by the zeros of d, so Strict mode needs d provably
            // nonzero. Never applies to a literal 0/0, in either mode.
            case Divide(Constant zero, var d) when zero.Value.IsZero && NonZeroForDomain(d, assumptions, mode):
                return new Constant(0);

            case Divide(var n, var d) when d.Equals(new Constant(1)):
                return n;

            case Divide(var numerator, var denominator)
                when TryReduceConstantFactors(numerator, denominator, assumptions, mode, cache, out Expr? reduced):
                return reduced!;

            // 1/i = i^-1 = -i: an i in the denominator moves up with a minus sign, a/(b*i) = -a*i/b.
            case Divide(var a, var d) when TryRemoveFactor(d, new ImaginaryUnit(), out Expr? rest):
                Expr moved = new Negate(new Multiply(a, new ImaginaryUnit()));
                return rest is null ? moved : new Divide(moved, rest);

            // Integer exponent: exact BigInteger power. 0^negative is undefined
            // (division by zero), so that combination is excluded and left symbolic.
            case Power(Constant b, Constant e)
                when e.Value.IsInteger && !(b.Value.IsZero && e.Value.Sign < 0):
                return new Constant(b.Value.Pow((int)e.Value.Numerator));

            // i^2 = -1, and on with period 4: i^3 = -i, i^4 = 1, i^-1 = -i.
            case Power(ImaginaryUnit, Constant e) when e.Value.IsInteger:
                return ImaginaryUnitPower(e.Value.Numerator);

            // Exponent p/q: exact result only if b is a perfect q-th power, 8^(2/3) = 4.
            // NOT approximated via Math.Pow - an inexact root stays symbolic rather than
            // silently becoming a "precise-looking" but wrong Rational. Negative bases are
            // left alone too: Power is the principal value, unlike the real-valued NthRoot.
            case Power(Constant b, Constant e)
                when !(b.Value.IsZero && e.Value.Sign < 0) &&
                     System.Numerics.BigInteger.Abs(e.Value.Numerator) <= MaxExactRootPower &&
                     TryExactRoot(b.Value, e.Value.Denominator, out Rational rootValue):
                return new Constant(rootValue.Pow((int)e.Value.Numerator));

            // b^(p/2) = b^((p-1)/2) * sqrt(b), for ExactValues to work out: 2^(3/2) = 2sqrt(2).
            case Power(Constant b, Constant e)
                when b.Value.Sign > 0 && e.Value.Denominator == 2 &&
                     System.Numerics.BigInteger.Abs(e.Value.Numerator) <= MaxExactRootPower:
                return new Multiply(
                    new Constant(b.Value.Pow((int)((e.Value.Numerator - 1) / 2))),
                    new Sqrt(b));

            case Power(var b, var e) when e.Equals(new Constant(0)):
                return new Constant(1);

            case Power(var b, var e) when e.Equals(new Constant(1)):
                return b;

            case Power(var b, var e) when b.Equals(new Constant(0)) && e.IsProvablyPositive(assumptions):
                return new Constant(0);

            // u^(1/2) is written sqrt(u), so that one value has one form: sqrt(y) - y^(1/2) = 0.
            case Power(var b, Constant e) when e.Value == Half:
                return new Sqrt(b);

            // sqrt(a)^2 = a; the left side is undefined for a < 0, so this enlarges the domain.
            case Power(Sqrt(var a), Constant e) when e.Value == 2 && NonNegativeForDomain(a, assumptions, mode):
                return a;

            case Power(Power(var b, var e1), var e2):
                return CollapsedPower(expr, b, e1, e2, assumptions, mode);

            // sqrt(x)^3 = x^(3/2), on the same terms as (x^(1/2))^3.
            case Power(Sqrt(var b), var e2) when b is not Constant:
                return CollapsedPower(expr, b, new Constant(Half), e2, assumptions, mode);

            // Still reachable: when CombineProductFactors may not add the exponents (Strict mode,
            // x^(1/2) * x^(1/2) would gain x < 0), the equal factors are squared as a whole instead.
            case Multiply(var b1, var b2) when b1.Equals(b2) && b1 is not Constant:
                return new Power(b1, new Constant(2));

            case Multiply(Divide(var a, var b), Divide(var c, var d)):
                return new Divide(new Multiply(a, c), new Multiply(b, d));

            case Multiply(Divide(var a, var b), var c) when c is not Divide:
                return new Divide(new Multiply(a, c), b);

            case Multiply(var c, Divide(var a, var b)) when c is not Divide:
                return new Divide(new Multiply(c, a), b);
                
            case Divide(Divide(var a, var b), var c):
                return new Divide(a, new Multiply(b, c));

            // a / (b/c) = a*c / b; the left side is undefined where c = 0, the right side isn't.
            case Divide(var a, Divide(var b, var c)) when NonZeroForDomain(c, assumptions, mode):
                return new Divide(new Multiply(a, c), b);

            // x^n / x^m = x^(n-m); here and below sqrt(x) counts as x^(1/2).
            case Divide(var n, var d)
                when IsPower(n, out Expr b1, out Expr e1) && IsPower(d, out Expr b2, out Expr e2) &&
                     b1.Equals(b2) && NonZeroForDomain(b1, assumptions, mode):
                return new Power(b1, new Subtract(e1, e2));

            // x^n / x = x^(n-1)
            case Divide(var n, var b2)
                when IsPower(n, out Expr b1, out Expr e1) && b1.Equals(b2) && NonZeroForDomain(b1, assumptions, mode):
                return new Power(b1, new Subtract(e1, new Constant(1)));

            // (x^n * c) / x = c * x^(n-1)
            case Divide(Multiply(var p, var c), var b2)
                when IsPower(p, out Expr b1, out Expr e1) && b1.Equals(b2) && NonZeroForDomain(b1, assumptions, mode):
                return new Multiply(c, new Power(b1, new Subtract(e1, new Constant(1))));

            // (c * x^n) / x = c * x^(n-1)
            case Divide(Multiply(var c, var p), var b2)
                when IsPower(p, out Expr b1, out Expr e1) && b1.Equals(b2) && NonZeroForDomain(b1, assumptions, mode):
                return new Multiply(c, new Power(b1, new Subtract(e1, new Constant(1))));

            // x / x^n = x^(1-n)
            case Divide(var b1, var d)
                when IsPower(d, out Expr b2, out Expr e2) && b1.Equals(b2) && NonZeroForDomain(b1, assumptions, mode):
                return new Power(b1, new Subtract(new Constant(1), e2));

            // sin(x)^2 + cos(x)^2 = 1, sec(x)^2 - tan(x)^2 = 1, csc(x)^2 - cot(x)^2 = 1 and their
            // variants live in SimplifySum, where the whole sum is visible at once.

            // ln(exp(a)) = a: exp(a) is always strictly positive for real a,
            // so ln is always defined on its result - no assumption needed.
            case Ln(Exp(var a)):
                return a;

            case Ln(Power(EulerNumber, var a)):
                return a;

            // ln(exp(u) * R) = u + ln(R), ln(R / exp(u)) = ln(R) - u: exp(u) > 0, so both sides
            // are defined for the same R. ln(e / (e + 1)) = 1 - ln(e + 1).
            case Ln(var a) when TryTakeExponentialsOut(a, out Expr? taken):
                return taken!;

            case Exp(Ln(var a)) when PositiveForDomain(a, assumptions, mode):
                return a;

            // exp(c ln(u)) = u^c, on the same terms as exp(ln(u)) = u.
            case Exp(var a) when IsLogarithmMultiple(a, out Expr? u, out Expr? c) && PositiveForDomain(u!, assumptions, mode):
                return new Power(u!, c!);

            case Power(EulerNumber, var a) when IsLogarithmMultiple(a, out Expr? u, out Expr? c) && PositiveForDomain(u!, assumptions, mode):
                return new Power(u!, c!);

            // exp(x + ln(y)) = exp(x) * y: the logarithms of a sum leave it on the same terms.
            // Undoes ln(exp(x) * y) = x + ln(y) inside exp(ln(exp(x) * y)), and
            // exp(x - ln(exp(x) + 1)) = exp(x) / (exp(x) + 1).
            case Exp(var a) when a is Add or Subtract && TryTakeLogarithmsOut(a, assumptions, mode, out Expr? product):
                return product!;

            case Power(EulerNumber, var a) when a is Add or Subtract && TryTakeLogarithmsOut(a, assumptions, mode, out Expr? product):
                return product!;

            // tan(x) = sin(x) / cos(x)
            case Divide(Sin(var x), Cos(var y)) when x.Equals(y):
                return new Tan(x);

            // cot(x) = cos(x) / sin(x)
            case Divide(Cos(var x), Sin(var y)) when x.Equals(y):
                return new Cot(x);

            // tan(x) * cot(x) = 1 only where both are defined: sin(x) != 0 and cos(x) != 0.
            case Multiply(Tan(var x), Cot(var y))
                when x.Equals(y) && NonZeroForDomain(new Sin(x), assumptions, mode) && NonZeroForDomain(new Cos(x), assumptions, mode):
                return new Constant(1);

            case Multiply(Cot(var x), Tan(var y))
                when x.Equals(y) && NonZeroForDomain(new Sin(x), assumptions, mode) && NonZeroForDomain(new Cos(x), assumptions, mode):
                return new Constant(1);

            // sin(x)^2 / cos(x)^2 = tan(x)^2
            case Divide(
                Power(Sin(var x1), Constant e1),
                Power(Cos(var x2), Constant e2))
                when e1.Value == 2 &&
                    e2.Value == 2 &&
                    x1.Equals(x2):
                return new Power(new Tan(x1), new Constant(2));

            // cos(x)^2 / sin(x)^2 = cot(x)^2
            case Divide(
                Power(Cos(var x1), Constant e1),
                Power(Sin(var x2), Constant e2))
                when e1.Value == 2 &&
                    e2.Value == 2 &&
                    x1.Equals(x2):
                return new Power(new Cot(x1), new Constant(2));

            // Exact perfect-square root; not a perfect square stays symbolic (falls through).
            case Sqrt(Constant c)
                when c.Value.Sign >= 0 && TryExactRoot(c.Value, 2, out Rational sqrtValue):
                return new Constant(sqrtValue);

            case Sqrt(Power(var b, Constant e)) when e.Value == 2:
                return b.IsProvablyNonNegative(assumptions)
                    ? b
                    : new Abs(b);

            // sqrt(4cosh(t)^2) = 2cosh(t), sqrt(x^4) = x^2: even powers leave the root where the
            // rest is known to be >= 0. Otherwise sqrt(-2x^2), defined at x = 0, would not be.
            case Sqrt(var a) when TryTakeSquaresOut(a, assumptions, out Expr? taken):
                return taken!;

            // sqrt(pi/4) = sqrt(pi)/2, sqrt(8x) = 2sqrt(2x): the square factors of the number
            // leave the root; a denominator isn't moved into it, so sqrt(pi/2) stays.
            case Sqrt(var a) when a is not Constant && ExtractCoefficient(a) is var (c, rest) && SquareFactor(c) is { } square:
                Rational left = c / (square * square);
                Expr inside = left.Numerator.IsOne ? rest : new Multiply(new Constant(new Rational(left.Numerator)), rest);
                Expr root = new Sqrt(left.Denominator.IsOne ? inside : new Divide(inside, new Constant(new Rational(left.Denominator))));
                Expr scaled = square.Numerator.IsOne ? root : new Multiply(new Constant(new Rational(square.Numerator)), root);
                return square.Denominator.IsOne ? scaled : new Divide(scaled, new Constant(new Rational(square.Denominator)));

            // Exact real n-th root; anything inexact stays symbolic (falls through).
            case NthRoot(Constant c, Constant n) when TryExactRealRoot(c.Value, n.Value, out Rational nthRootValue):
                return new Constant(nthRootValue);

            // sin(pi/6) = 1/2, asin(1/2) = pi/6, sqrt(8) = 2sqrt(2), ln(1) = 0 and similar.
            case var _ when ExactValues.TryEvaluate(expr, out Expr? exact):
                return exact;

            // 1/(sqrt(3) + 2) = 2 - sqrt(3): a number over a + b sqrt(d) times the conjugate.
            case Divide(var n, var d)
                when QuadraticSurd.Of(n) is { } top && QuadraticSurd.Of(d) is { B.IsZero: false } bottom && top / bottom is { } quotient:
                return quotient.ToExpr();

            // ln(1/2) = -ln(2), ln(9) = 2ln(3), ln(4/9) = 2ln(2/3); ln(2/3) and ln(6) stay.
            case Ln(Constant c) when c.Value.Sign > 0 && LogarithmOfRational(c.Value) is { } log:
                return log;

            case Abs(Constant c):
                return new Constant(c.Value.Abs());

            case Abs(var a) when QuadraticSurd.Of(a) is { B.IsZero: false } surd:
                return surd.Abs.ToExpr();

            case Abs(var a) when a is Abs:
                return a;

            case Abs(var a) when a.IsProvablyNonNegative(assumptions):
                return a;

            case Abs(var a) when a.IsProvablyNegative(assumptions):
                return new Negate(a);

            case Sign(Constant c):
                return new Constant(c.Value.Sign);

            case Sign(var a) when QuadraticSurd.Of(a) is { B.IsZero: false } surd:
                return new Constant(surd.Sign);

            case Sign(var a) when a.IsProvablyPositive(assumptions):
                return new Constant(1);

            case Sign(var a) when a.IsProvablyNegative(assumptions):
                return new Constant(-1);

            case Floor(Constant c):
                return new Constant(c.Value.Floor());

            case Ceiling(Constant c):
                return new Constant(c.Value.Ceiling());

            case Round(Constant c):
                return new Constant(c.Value.Round());

            case Min(Constant a, Constant b):
                return new Constant(a.Value < b.Value ? a.Value : b.Value);

            case Max(Constant a, Constant b):
                return new Constant(a.Value > b.Value ? a.Value : b.Value);

            // An inverse after its function: x for real x, or where the inverse is defined.
            case Tan(Atan(var a)):
                return a;

            case Sinh(Asinh(var a)):
                return a;

            case Asinh(Sinh(var a)):
                return a;

            case Atanh(Tanh(var a)):
                return a;

            case Acosh(Cosh(var a)):
                return new Abs(a);

            case Sin(Asin(var a)) when InDomainForComposition(a, -1, 1, mode):
                return a;

            case Cos(Acos(var a)) when InDomainForComposition(a, -1, 1, mode):
                return a;

            case Tanh(Atanh(var a)) when InDomainForComposition(a, -1, 1, mode):
                return a;

            case Cosh(Acosh(var a)) when InDomainForComposition(a, 1, double.PositiveInfinity, mode):
                return a;

            // atan(tan(10)) = 10 - 3pi, asin(sin(c)) = (-1)^k (c - k pi), acos(cos(c)) = |c - 2m pi|.
            case Atan(Tan(var c)) when TryTurns(c, Math.PI, out long k, out _):
                return new Subtract(c, new Multiply(new Constant(k), new Pi()));

            case Asin(Sin(var c)) when TryTurns(c, Math.PI, out long k, out _):
                Expr halfTurns = new Multiply(new Constant(k), new Pi());
                return k % 2 == 0 ? new Subtract(c, halfTurns) : new Subtract(halfTurns, c);

            case Acos(Cos(var c)) when TryTurns(c, 2 * Math.PI, out long m, out double offset) && Math.Abs(offset) > TurnMargin:
                Expr turns = new Multiply(new Constant(2 * m), new Pi());
                return offset > 0 ? new Subtract(c, turns) : new Subtract(turns, c);

            // sin(-u) = -sin(u), cos(-u) = cos(u). A sum keeps its form: sin(1 - x) stays.
            case UnaryExpr f when IsOdd(f) && HasNegativeSign(f.Argument):
                return new Negate(f.WithChildren([new Negate(f.Argument)]));

            case UnaryExpr f when f is Cos or Sec or Cosh or Sech or Abs && HasNegativeSign(f.Argument):
                return f.WithChildren([new Negate(f.Argument)]);

            case NthRoot(var a, Constant n) when IsOddInteger(n.Value) && HasNegativeSign(a):
                return new Negate(new NthRoot(new Negate(a), n));

            case Power(var b, Constant k) when k.Value.IsInteger && HasNegativeSign(b):
                Expr power = new Power(new Negate(b), k);
                return k.Value.Numerator.IsEven ? power : new Negate(power);

            default:
                return expr;
        }
    }

    // a = c ln(u) with exactly one logarithm among the factors: -ln(2), ln(x)*y, ln(2)/2.
    private static bool IsLogarithmMultiple(Expr a, out Expr? u, out Expr? c)
    {
        u = c = null;
        switch (a)
        {
            case Ln(var v):
                (u, c) = (v, new Constant(1));
                return true;

            case Negate(var t) when IsLogarithmMultiple(t, out u, out Expr? k):
                c = new Negate(k!);
                return true;

            case Divide(var t, var d) when IsLogarithmMultiple(t, out u, out Expr? k):
                c = new Divide(k!, d);
                return true;

            case Multiply(var l, var r):
                if (IsLogarithmMultiple(l, out u, out Expr? kl) && !IsLogarithmMultiple(r, out _, out _))
                {
                    c = new Multiply(kl!, r);
                    return true;
                }
                if (IsLogarithmMultiple(r, out u, out Expr? kr) && !IsLogarithmMultiple(l, out _, out _))
                {
                    c = new Multiply(l, kr!);
                    return true;
                }
                u = null;
                return false;

            default:
                return false;
        }
    }

    // exp(a) of a sum with terms c ln(u) for numbers c: exp(x + 2ln(y)) = exp(x) * y^2.
    private static bool TryTakeLogarithmsOut(Expr a, Assumptions assumptions, SimplifyMode mode, out Expr? product)
    {
        product = null;
        var rest = new List<(Rational, Expr)>();
        var top = new List<Expr>();
        var bottom = new List<Expr>();
        Rational number = Rational.Zero;
        foreach (var (coefficient, term) in CollectTerms(a, term => term))
        {
            if (term is Constant c)
                number += coefficient * c.Value;
            // Only numbers times a logarithm, like SymPy: exp(1 + x ln(2)) stays, and so do the
            // forms that Calculus Gruntz expands in, exp(-x + exp(w) ln(1/w)).
            else if (term is Ln(var u) && PositiveForDomain(u, assumptions, mode))
            {
                // exp(x - ln(y)) = exp(x) / y, not exp(x) * y^-1.
                Rational scale = coefficient.Abs();
                Expr power = scale.IsOne ? u : new Power(u, new Constant(scale));
                (coefficient.Sign < 0 ? bottom : top).Add(power);
            }
            else
                rest.Add((coefficient, term));
        }
        if (top.Count == 0 && bottom.Count == 0)
            return false;

        if (rest.Count > 0 || !number.IsZero)
            top.Insert(0, new Exp(BuildSum(rest, number)));
        static Expr Product(List<Expr> factors) =>
            factors.Count == 0 ? new Constant(1) : factors.Aggregate((p, factor) => new Multiply(p, factor));
        product = bottom.Count == 0 ? Product(top) : new Divide(Product(top), Product(bottom));
        return true;
    }

    // The exponent of a power of e; null for anything else.
    private static Expr? ExponentOfE(Expr factor) => factor switch
    {
        Exp(var u) => u,
        Power(EulerNumber, var u) => u,
        EulerNumber => new Constant(1),
        Sqrt(EulerNumber) => new Constant(Half),
        Sqrt(Exp(var u)) => Scaled(u, Half),
        Power(Exp(var u), Constant k) => Scaled(u, k.Value),
        Power(Exp(var u), var k) => new Multiply(k, u),
        _ => null,
    };

    private static Expr Scaled(Expr u, Rational k) =>
        u is Constant c ? new Constant(c.Value * k) : new Multiply(new Constant(k), u);

    // ln(a) with the powers of e of a product or quotient taken out: ln(exp(x) * y / e) =
    // x - 1 + ln(y), ln(exp(x) / y) = x - ln(y).
    private static bool TryTakeExponentialsOut(Expr a, out Expr? result)
    {
        Expr? sum = null;
        var top = new List<Expr>();
        var bottom = new List<Expr>();
        bool negative = false;

        void Split(Expr e, bool below)
        {
            switch (e)
            {
                case Multiply(var l, var r):
                    Split(l, below);
                    Split(r, below);
                    break;
                case Divide(var n, var d):
                    Split(n, below);
                    Split(d, !below);
                    break;
                case Negate(var inner):
                    negative = !negative;
                    Split(inner, below);
                    break;
                default:
                    if (ExponentOfE(e) is not { } u)
                        (below ? bottom : top).Add(e);
                    else if (below)
                        sum = sum is null ? new Negate(u) : new Subtract(sum, u);
                    else
                        sum = sum is null ? u : new Add(sum, u);
                    break;
            }
        }

        Split(a, false);
        result = sum;
        if (sum is null)
            return false;

        static Expr Product(List<Expr> factors) =>
            factors.Count == 0 ? new Constant(1) : factors.Aggregate((product, factor) => new Multiply(product, factor));

        Expr Signed(Expr e) => negative ? new Negate(e) : e;

        if (top.Count == 0 && bottom.Count > 0)
            result = new Subtract(sum, new Ln(Signed(Product(bottom))));
        else if (top.Count > 0 || negative)
            result = new Add(sum, new Ln(Signed(bottom.Count == 0 ? Product(top) : new Divide(Product(top), Product(bottom)))));
        return true;
    }

    // Generic mode: anything but a number outside [low, high]; Strict mode: a number inside.
    private static bool InDomainForComposition(Expr a, double low, double high, SimplifyMode mode) =>
        TryEvaluateConstant(a, out double value)
            ? value >= low && value <= high
            : mode == SimplifyMode.Generic;

    // Rounding c / period is exact only well away from a half, where the inverse jumps, and for
    // moderate c: then atan(tan(c)) = c - k pi for the nearest k.
    private const double MaxTurns = 1e6;
    private const double TurnMargin = 1e-9;

    private static bool TryTurns(Expr c, double period, out long k, out double offset)
    {
        (k, offset) = (0, 0);
        if (!TryEvaluateConstant(c, out double value))
            return false;

        double turns = value / period;
        if (!double.IsFinite(turns) || Math.Abs(turns) > MaxTurns)
            return false;

        double nearest = Math.Round(turns);
        offset = turns - nearest;
        if (Math.Abs(Math.Abs(offset) - 0.5) < TurnMargin)
            return false;

        k = (long)nearest;
        return true;
    }

    private static bool TryEvaluateConstant(Expr e, out double value)
    {
        value = double.NaN;
        if (e.GetVariables().Count > 0)
            return false;
        try
        {
            value = e.Evaluate(new Dictionary<string, double>());
        }
        catch (Exception error) when (error is InvalidOperationException or ArithmeticException or NotSupportedException)
        {
            return false;
        }
        return double.IsFinite(value);
    }

    private static bool IsOdd(UnaryExpr f) =>
        f is Sin or Tan or Cot or Csc or Sinh or Tanh or Coth or Csch or Asin or Atan or Asinh or Atanh or Sign;

    // -u, a negative number, a product with a negative coefficient or a quotient with one on top.
    private static bool HasNegativeSign(Expr u) =>
        ConstantFactor(u).Sign < 0 || u is Divide(var n, _) && ConstantFactor(n).Sign < 0;

    // Removes one occurrence of `factor` from a product: (2 * i, i) -> 2. `rest` is null when
    // the product was the factor itself.
    private static bool TryRemoveFactor(Expr product, Expr factor, out Expr? rest)
    {
        rest = null;
        if (product.Equals(factor))
            return true;

        if (product is not Multiply(var left, var right))
            return false;

        if (TryRemoveFactor(left, factor, out Expr? leftRest))
        {
            rest = leftRest is null ? right : new Multiply(leftRest, right);
            return true;
        }

        if (TryRemoveFactor(right, factor, out Expr? rightRest))
        {
            rest = rightRest is null ? left : new Multiply(left, rightRest);
            return true;
        }

        return false;
    }

    private static Expr ImaginaryUnitPower(System.Numerics.BigInteger n) =>
        (int)((n % 4 + 4) % 4) switch
        {
            0 => new Constant(1),
            1 => new ImaginaryUnit(),
            2 => new Constant(-1),
            _ => new Negate(new ImaginaryUnit())
        };

    private static readonly Rational Half = new(1, 2);

    // b^e, with sqrt(b) taken as b^(1/2). The root of a number stays a factor of its own, for
    // ExactValues: sqrt(2)/2 is not 2^(-1/2).
    private static bool IsPower(Expr expr, out Expr baseExpr, out Expr exponent)
    {
        (baseExpr, exponent) = expr switch
        {
            Power(var b, var e) => (b, e),
            Sqrt(var b) when b is not Constant => (b, new Constant(Half)),
            _ => (expr, new Constant(1))
        };
        return expr is Power || expr is Sqrt(not Constant);
    }

    // Any factor as a base and an exponent: x^2 -> (x, 2), sqrt(x) -> (x, 1/2), y -> (y, 1).
    private static (Expr Base, Expr Exponent) AsPower(Expr factor)
    {
        IsPower(factor, out Expr b, out Expr e);
        return (b, e);
    }

    // (b^e1)^e2 = b^(e1*e2) where both sides agree; otherwise the expression is returned unchanged.
    private static Expr CollapsedPower(Expr expr, Expr b, Expr e1, Expr e2, Assumptions assumptions, SimplifyMode mode)
    {
        // Safe to collapse unconditionally only when e1 is an odd integer (sign-preserving:
        // x -> x^e1 never erases the sign of b, so composing exponents afterward can't lose it).
        // Additionally, (x^-1)^-1 = x would become defined at x = 0, so a negative inner
        // exponent needs either a nonzero base or a positive outer exponent.
        Expr combined = new Power(b, new Multiply(e1, e2));
        bool zeroBaseSafe = NonZeroForDomain(b, assumptions, mode) ||
                            e1.IsProvablyPositive(assumptions) ||
                            e2.IsProvablyPositive(assumptions);
        if (!zeroBaseSafe)
            return expr;
        // Integer powers compose for any base: (x^2)^3 = x^6, (x^-1)^2 = x^-2.
        bool innerIsInteger = e1 is Constant ce1 && ce1.Value.IsInteger;
        bool outerIsInteger = e2 is Constant ce2 && ce2.Value.IsInteger;
        if (innerIsInteger && outerIsInteger)
            return combined;
        // Power is the principal value: a non-integer exponent of a negative base is
        // undefined over the reals. So (x^3)^(1/3) is undefined for x < 0 while x is not -
        // an odd inner exponent with a non-integer outer one only enlarges the domain.
        if (e1 is Constant ce1Odd && IsOddInteger(ce1Odd.Value) && mode == SimplifyMode.Generic)
            return combined;
        // A non-integer inner exponent (x^(1/2), x^(1/3)) is undefined for b < 0, so
        // collapsing only enlarges the domain - fine in Generic mode. An even integer
        // inner exponent is different: (x^2)^(1/2) = |x|, not x, so no shortcut.
        bool innerUndefinedForNegativeBase = e1 is Constant ce1NonInt && !ce1NonInt.Value.IsInteger;
        return b.IsProvablyNonNegative(assumptions) ||
               (mode == SimplifyMode.Generic && innerUndefinedForNegativeBase)
            ? combined
            : expr;
    }

    // BigInteger.IsEven instead of a (long) cast, which overflows for huge integers.
    private static bool IsOddInteger(Rational value) =>
        value.IsInteger && !value.Numerator.IsEven;

    // Guards c^q in TryExactRealRoot against building astronomically large numbers.
    private const int MaxExactRootPower = 1024;

    // nthroot(c, p/q) = c^(q/p), computed exactly: raise |c| to q, take the |p|-th root,
    // invert for negative p. A negative c only has a real root for an odd integer degree
    // (same convention as NthRoot.Evaluate); otherwise it's undefined and left symbolic.
    private static bool TryExactRealRoot(Rational value, Rational degree, out Rational root)
    {
        root = default;
        if (degree.IsZero)
            return false;

        bool negativeBase = value.Sign < 0;
        if (negativeBase && !IsOddInteger(degree))
            return false;

        if (degree.Denominator > MaxExactRootPower)
            return false;

        Rational powered = value.Abs().Pow((int)degree.Denominator);
        if (!TryExactRoot(powered, System.Numerics.BigInteger.Abs(degree.Numerator), out Rational magnitude))
            return false;

        if (degree.Sign < 0)
        {
            if (magnitude.IsZero)
                return false; // nthroot(0, -n) = 1/0
            magnitude = Rational.One / magnitude;
        }

        root = negativeBase ? -magnitude : magnitude;
        return true;
    }

    // x^p * x^q = x^(p+q) must not enlarge the domain: x^-1 * x^2 is undefined at x = 0
    // but x^1 isn't, and x^(1/2) * x^(1/2) is undefined for x < 0 but x isn't.
    // Returns true only when both sides are defined at exactly the same points.
    private static bool CanMergeExponents(Expr baseExpr, Expr e1, Expr e2, Assumptions assumptions, SimplifyMode mode)
    {
        // Generic mode: x^p * x^q = x^(p+q) holds wherever the left side is defined.
        if (mode == SimplifyMode.Generic)
            return true;

        // Every real power of a positive base is defined.
        if (baseExpr.IsProvablyPositive(assumptions))
            return true;

        if (e1 is not Constant c1 || e2 is not Constant c2)
            return false; // symbolic exponents: sign/integrality unknown

        Rational p = c1.Value, q = c2.Value;

        if (p.IsInteger && q.IsInteger)
        {
            // Integer powers are defined for any nonzero base. At 0, both sides agree
            // as long as the exponents share a sign (both defined or both undefined).
            bool sameSign = (p.Sign >= 0 && q.Sign >= 0) || (p.Sign <= 0 && q.Sign <= 0);
            return sameSign || baseExpr.IsProvablyNonZero(assumptions);
        }

        if (p.Sign >= 0 && q.Sign >= 0)
        {
            // Non-negative exponents are all defined at 0 and for a non-negative base.
            if (baseExpr.IsProvablyNonNegative(assumptions))
                return true;

            // For a negative base: a non-integer sum means at least one factor was already
            // non-integer, so the left side is no more defined than the right side.
            // An integer sum (1/2 + 1/2) would newly define x^1 for x < 0 - not safe.
            return !(p + q).IsInteger;
        }

        return false;
    }

    private static bool TryIntegerNthRoot(System.Numerics.BigInteger value, int n, out System.Numerics.BigInteger root)
    {
        root = System.Numerics.BigInteger.Zero;
        if (value.Sign < 0 || n <= 0) return false;
        if (value.IsZero) return true;
        if (n == 1) { root = value; return true; }

        System.Numerics.BigInteger low = 0, high = value;
        while (low <= high)
        {
            System.Numerics.BigInteger mid = (low + high) / 2;
            System.Numerics.BigInteger midPow = System.Numerics.BigInteger.Pow(mid, n);

            if (midPow == value) { root = mid; return true; }
            if (midPow < value) low = mid + 1;
            else high = mid - 1;
        }

        return false;
    }

    // Exact n-th root of a non-negative rational: numerator and denominator
    // (coprime by Rational's construction) must each be a perfect n-th power.
    private static bool TryExactRoot(Rational value, System.Numerics.BigInteger n, out Rational root)
    {
        root = default;
        if (value.Sign < 0 || n <= 0) return false;

        int nn;
        try { nn = (int)n; }
        catch (OverflowException) { return false; }

        if (!TryIntegerNthRoot(value.Numerator, nn, out System.Numerics.BigInteger numRoot)) return false;
        if (!TryIntegerNthRoot(value.Denominator, nn, out System.Numerics.BigInteger denRoot)) return false;

        root = new Rational(numRoot, denRoot);
        return true;
    }

    // Recurses through Negate, so -(2x) is the term x with coefficient -2 and merges with x.
    private static (Rational Coefficient, Expr Term) ExtractCoefficient(Expr expr) => expr switch
    {
        Negate(var t) when ExtractCoefficient(t) is var (c, inner) => (-c, inner),
        Multiply(Constant c, var t) => (c.Value, t),
        Multiply(var t, Constant c) => (c.Value, t),
        // (2 * sin(t)^2) * R, the chain that Canonicalize builds: the 2 is deeper on the left.
        Multiply(Multiply l, var r) when ExtractCoefficient(l) is ({ IsOne: false } c, var inner) => (c, new Multiply(inner, r)),
        Divide(var t, Constant c) when !c.Value.IsZero => (Rational.One / c.Value, t),
        _ => (Rational.One, expr)
    };

    // Flattens a chain of Add/Subtract into a flat list of (coefficient, term) pairs, in order,
    // simplifying each term first. A term that simplifies to a sum (ln(exp(a + b))) is flattened
    // too; its own terms are simplified already. Uses an explicit stack, not recursion: a sum of
    // n terms is a chain n levels deep.
    private static List<(Rational Coefficient, Expr Term)> CollectTerms(Expr sum, Func<Expr, Expr> simplifyTerm)
    {
        var terms = new List<(Rational Coefficient, Expr Term)>();
        var pending = new Stack<(Expr Expr, Rational Sign, bool Simplified)>();
        pending.Push((sum, Rational.One, false));

        while (pending.TryPop(out var item))
        {
            switch (item.Expr)
            {
                case Add(var l, var r):
                    pending.Push((r, item.Sign, item.Simplified));
                    pending.Push((l, item.Sign, item.Simplified));
                    break;
                case Subtract(var l, var r):
                    pending.Push((r, -item.Sign, item.Simplified));
                    pending.Push((l, item.Sign, item.Simplified));
                    break;
                default:
                    Expr simplified = item.Simplified ? item.Expr : simplifyTerm(item.Expr);
                    if (simplified is Add or Subtract)
                    {
                        pending.Push((simplified, item.Sign, true));
                        break;
                    }

                    var (coef, term) = ExtractCoefficient(simplified);
                    terms.Add((coef * item.Sign, term));
                    break;
            }
        }

        return terms;
    }

    private static bool IsSquareOf<TFunction>(Expr term, out Expr argument) where TFunction : UnaryExpr
    {
        if (term is Power(TFunction f, Constant e) && e.Value == 2)
        {
            argument = f.Argument;
            return true;
        }

        argument = null!;
        return false;
    }

    private static Expr Square(Expr expr) => new Power(expr, new Constant(2));

    // The identities p^2 = 1 + s*q^2: sin^2 = 1 - cos^2, sec^2 = 1 + tan^2, csc^2 = 1 + cot^2,
    // cosh^2 = 1 + sinh^2, sech^2 = 1 - tanh^2, coth^2 = 1 + csch^2. For a square of one of
    // these functions: whether it is p, the other square, s, and the function that must not be 0
    // for both to be defined (null where they are defined everywhere).
    private static bool TryGetPythagoreanIdentity(Expr square, out bool isP, out Expr other, out int sign, out Expr? nonZero)
    {
        (bool, Expr?, int, Expr?) identity = square is Power(UnaryExpr f, Constant { Value: var e }) && e == 2 ? f switch
        {
            Sin(var x) => (true, Square(new Cos(x)), -1, null),
            Cos(var x) => (false, Square(new Sin(x)), -1, null),
            Sec(var x) => (true, Square(new Tan(x)), 1, new Cos(x)),
            Tan(var x) => (false, Square(new Sec(x)), 1, new Cos(x)),
            Csc(var x) => (true, Square(new Cot(x)), 1, new Sin(x)),
            Cot(var x) => (false, Square(new Csc(x)), 1, new Sin(x)),
            Cosh(var x) => (true, Square(new Sinh(x)), 1, null),
            Sinh(var x) => (false, Square(new Cosh(x)), 1, null),
            Sech(var x) => (true, Square(new Tanh(x)), -1, null),
            Tanh(var x) => (false, Square(new Sech(x)), -1, null),
            Coth(var x) => (true, Square(new Csch(x)), 1, new Sinh(x)),
            Csch(var x) => (false, Square(new Coth(x)), 1, new Sinh(x)),
            _ => default,
        } : default;
        (isP, other, sign, nonZero) = (identity.Item1, identity.Item2!, identity.Item3, identity.Item4);
        return sign != 0;
    }

    // A term as F * p^2 for every factor p^2 of it that is in an identity: R^2 cos(t)^2 is
    // (R^2, cos(t)^2), and a bare square has F = 1.
    private static IEnumerable<(Expr F, Expr Square)> PythagoreanSquares(Expr term)
    {
        if (TryGetPythagoreanIdentity(term, out _, out _, out _, out _))
        {
            yield return (new Constant(1), term);
            yield break;
        }
        if (term is not Multiply)
            yield break;

        var factors = new List<Expr>();
        Rational ignored = Rational.One;
        FlattenProduct(term, factors, ref ignored);
        for (int i = 0; i < factors.Count; i++)
        {
            if (!TryGetPythagoreanIdentity(factors[i], out _, out _, out _, out _))
                continue;
            var rest = factors.Where((_, k) => k != i).ToList();
            Expr f = rest.Count == 1 ? rest[0] : rest.Aggregate((product, factor) => new Multiply(product, factor)).Canonicalize();
            yield return (f, factors[i]);
        }
    }

    private static bool HasPythagoreanFunction(Expr e) =>
        e is Sin or Cos or Tan or Cot or Sec or Csc or Sinh or Cosh or Tanh or Coth or Sech or Csch
        || e.Children.Any(HasPythagoreanFunction);

    private static Expr Times(Expr f, Expr square) => f is Constant { Value.IsOne: true } ? square : new Multiply(f, square).Canonicalize();

    // The identities above applied to the whole flattened sum rather than to a fixed two-node
    // shape, so they work with other terms in between, any coefficients, a common factor F and
    // any order: sin^2 + cos^2 + 1 -> 2, 2sin^2 + 3cos^2 -> 2 + cos^2, 1 - sin^2 -> cos^2,
    // 2sec^2 - tan^2 -> sec^2 + 1, R^2 cos^2 + R^2 sin^2 -> R^2, 1 + sinh^2 -> cosh^2.
    private static void ApplyPythagoreanIdentities(
        List<(Rational Coefficient, Expr Term)> terms, Dictionary<Expr, int> termIndex, ref Rational constant,
        Assumptions assumptions, SimplifyMode mode)
    {
        var squares = new Dictionary<(Expr F, Expr Square), int>();
        void Register(int i)
        {
            foreach (var key in PythagoreanSquares(terms[i].Term))
                squares.TryAdd(key, i);
        }
        for (int i = 0; i < terms.Count; i++)
            Register(i);
        if (squares.Count == 0)
            return;

        // c F, which is the constant for F = 1.
        Rational partCoefficient(Expr f, Rational c) =>
            f is Constant { Value.IsOne: true } ? c : termIndex.TryGetValue(f, out int k) ? terms[k].Coefficient : Rational.Zero;
        void AddPart(Expr f, Rational c, ref Rational constantSum)
        {
            if (f is Constant { Value.IsOne: true })
                constantSum += c;
            else if (termIndex.TryGetValue(f, out int k))
                terms[k] = (terms[k].Coefficient + c, terms[k].Term);
            else
            {
                termIndex[f] = terms.Count;
                terms.Add((c, f));
                Register(terms.Count - 1);
            }
        }

        // Step 1: where both F p^2 and F q^2 occur, the one with the smaller coefficient is
        // rewritten in terms of the other (on a tie, q is kept):
        //   a*p^2 + b*q^2 = a + (s*a + b)*q^2 = -s*b + (a + s*b)*p^2.
        // For sin/cos the square that is kept gets a positive coefficient. If both cancel
        // (sec^2 - tan^2 = 1), the left side was undefined where cos(x) = 0 (sin(x) = 0 for
        // csc/cot, x = 0 for coth/csch) and the right side isn't, so Strict mode needs that
        // excluded by the assumptions.
        for (int i = 0; i < terms.Count; i++)
        {
            foreach (var (f, pSquare) in PythagoreanSquares(terms[i].Term).ToList())
            {
                Rational a = terms[i].Coefficient;
                if (a.IsZero || !TryGetPythagoreanIdentity(pSquare, out bool isP, out Expr qSquare, out int s, out Expr? nonZero) || !isP)
                    continue;
                if (!squares.TryGetValue((f, qSquare), out int j) || j == i || terms[j].Coefficient.IsZero)
                    continue;

                Rational b = terms[j].Coefficient;
                Rational sa = s > 0 ? a : -a, sb = s > 0 ? b : -b;

                bool keepP = b < a;
                Rational keptCoefficient = keepP ? a + sb : sa + b;
                if (keptCoefficient.IsZero && nonZero is not null && !NonZeroForDomain(nonZero, assumptions, mode))
                    continue;

                if (keepP)
                {
                    terms[i] = (keptCoefficient, terms[i].Term);
                    terms[j] = (Rational.Zero, terms[j].Term);
                    AddPart(f, -sb, ref constant);
                }
                else
                {
                    terms[i] = (Rational.Zero, terms[i].Term);
                    terms[j] = (keptCoefficient, terms[j].Term);
                    AddPart(f, a, ref constant);
                }
            }
        }

        // Step 2: c F + s c F q^2 = c F p^2 and c F - c F p^2 = -s c F q^2: 1 - sin^2 = cos^2,
        // 1 + sinh^2 = cosh^2, cosh^2 - 1 = sinh^2. Runs after step 1, so at most one of p^2
        // and q^2 is left for each F and argument, and this can't undo step 1. Both sides are
        // defined at the same points. An F with these functions in it is left alone, or the
        // expansion sin - sin cos^2 of (1 - cos^2) sin would fold back into sin^3.
        for (int i = 0; i < terms.Count; i++)
        {
            foreach (var (f, square) in PythagoreanSquares(terms[i].Term).ToList())
            {
                if (HasPythagoreanFunction(f))
                    continue;
                Rational coefficient = terms[i].Coefficient;
                Rational c = partCoefficient(f, constant);
                if (coefficient.IsZero || c.IsZero || !TryGetPythagoreanIdentity(square, out bool isP, out Expr other, out int s, out _))
                    continue;
                if (coefficient != (isP ? -c : s * c))
                    continue;

                terms[i] = (isP ? -s * c : c, Times(f, other));
                AddPart(f, -c, ref constant);
                break;
            }
        }
    }

    // The product counterpart of SimplifySum: works on a whole Multiply chain instead
    // of one binary node, so repeated factors merge even when they aren't adjacent in the
    // tree - x*y*x*y -> x^2*y^2 and 2*x*3*x -> 6*x^2 (the binary rules only caught the
    // first pair). Constants are multiplied together; equal bases get their exponents
    // added, subject to the same domain rules as the binary rules (CanMergeExponents).
    // A negated product is one too: -(-2y^2) = 2y^2, -(2x) = -2x.
    private static Expr CombineProductFactors(Expr expr, Assumptions assumptions, SimplifyMode mode)
    {
        if (expr is not (Multiply or Negate(Multiply)))
            return expr;

        var factors = new List<Expr>();
        Rational coefficient = Rational.One;
        FlattenProduct(expr, factors, ref coefficient);

        var groups = new List<(Expr Base, Expr Exponent)>();

        foreach (Expr factor in factors)
        {
            if (factor is Constant c)
            {
                coefficient *= c.Value;
                continue;
            }

            var (baseExpr, exponent) = AsPower(factor);

            // Merge into the first group with the same base that allows it; otherwise start
            // a new group (e.g. x and x^-1 stay apart in Strict mode unless x != 0 is known).
            int index = groups.FindIndex(g =>
                g.Base.Equals(baseExpr) && CanMergeExponents(baseExpr, g.Exponent, exponent, assumptions, mode));

            if (index >= 0)
                groups[index] = (baseExpr, AddExponents(groups[index].Exponent, exponent));
            else
                groups.Add((baseExpr, exponent));
        }

        // 0 * anything is left to the dedicated rule in ApplyRules.
        if (coefficient.IsZero)
            return expr;

        // A lone -1 is written as a negation (-(x^2*y)) rather than -1 * x^2 * y.
        bool negate = coefficient == Rational.MinusOne;
        Expr? result = coefficient.IsOne || negate ? null : new Constant(coefficient);
        foreach (var (baseExpr, exponent) in groups)
        {
            Expr term = exponent is Constant e && (e.Value.IsOne || e.Value == Half)
                ? e.Value.IsOne ? baseExpr : new Sqrt(baseExpr)
                : new Power(baseExpr, exponent);
            result = result is null ? term : new Multiply(result, term);
        }

        result ??= new Constant(negate ? Rational.One : coefficient);
        return negate ? new Negate(result) : result;
    }

    // Negations anywhere in the chain are pulled out into the coefficient, so that
    // -x * y * x merges to -(x^2*y) and x * (-y) * x * (-y) to x^2 * y^2.
    private static void FlattenProduct(Expr expr, List<Expr> factors, ref Rational coefficient)
    {
        switch (expr)
        {
            case Multiply(var l, var r):
                FlattenProduct(l, factors, ref coefficient);
                FlattenProduct(r, factors, ref coefficient);
                break;

            case Negate(var inner):
                coefficient = -coefficient;
                FlattenProduct(inner, factors, ref coefficient);
                break;

            default:
                factors.Add(expr);
                break;
        }
    }

    private static Expr AddExponents(Expr e1, Expr e2) =>
        e1 is Constant c1 && e2 is Constant c2
            ? new Constant(c1.Value + c2.Value)
            : new Add(e1, e2);

    // A sum is simplified as a whole: each term of the Add/Subtract chain is simplified (through
    // the cache), then like terms, constants and the trigonometric identities are combined over
    // all of them in one pass. Simplifying every prefix of the chain as a node of its own, as for
    // other nodes, took time growing with the square of the number of terms and recursed once per
    // term; binary rules such as sec^2 - tan^2 = 1 also fired only on neighbouring terms.
    private static Expr SimplifySum(Expr sum, Assumptions assumptions, SimplifyMode mode, Dictionary<Expr, Expr> cache)
    {
        var raw = CollectTerms(sum, term => SimplifyCached(term, assumptions, mode, cache));

        Rational constantSum = Rational.Zero;
        var combined = new List<(Rational Coefficient, Expr Term)>();
        var termIndex = new Dictionary<Expr, int>();

        foreach (var (coef, term) in raw)
        {
            if (term is Constant c)
            {
                constantSum += coef * c.Value;
                continue;
            }

            if (termIndex.TryGetValue(term, out int existingIndex))
            {
                var (existingCoef, existingTerm) = combined[existingIndex];
                combined[existingIndex] = (existingCoef + coef, existingTerm);
            }
            else
            {
                termIndex[term] = combined.Count;
                combined.Add((coef, term));
            }
        }

        ApplyPythagoreanIdentities(combined, termIndex, ref constantSum, assumptions, mode);

        CollectNegatedSums(combined);
        CollectNumericMultiples(combined);
        combined.RemoveAll(t => t.Coefficient.IsZero);
        MergeLogarithms(combined);
        MergeExponentialLogarithms(combined, ref constantSum);
        CombineFractions(combined, ref constantSum, mode);
        Expr result = BuildSum(combined, constantSum);
        if (SurdSum(combined, constantSum) is { } surd && surd.ToExpr() is var folded && NodeCount(folded) < NodeCount(result))
            return folded;

        // An unchanged sum keeps its instance, and with it the cached hash codes.
        return result.Equals(sum) ? sum : result;
    }

    // Expand simplifies, and the sums it simplifies are put over a common denominator by the
    // caller, not inside.
    [ThreadStatic]
    private static bool t_combiningFractions;

    // Terms linked by a base in a denominator, x/(x + 1) and 1/(x + 1) or sqrt(r) and x/sqrt(r),
    // are put over a common denominator, with the numerator multiplied out, where something then
    // cancels: x/(x + 1) + 1/(x + 1) = 1, sqrt(x^2 + 1) - x^2/sqrt(x^2 + 1) = 1/sqrt(x^2 + 1), and
    // a Laplacian whose terms add up to 0 is 0. Where nothing cancels, the terms keep their form:
    // exp(c)/c - 1/c, 1/x + 1/y.
    private static void CombineFractions(List<(Rational Coefficient, Expr Term)> terms, ref Rational constant, SimplifyMode mode)
    {
        if (t_combiningFractions || !terms.Any(t => HasDenominator(t.Term)))
            return;

        foreach (List<int> component in LinkedByDenominators(terms))
        {
            Expr after;
            Rational weight;
            bool cancelled;
            t_combiningFractions = true;
            try
            {
                after = OverCommonDenominator(
                    [.. component.Select(i => FractionParts.Of(terms[i].Coefficient, terms[i].Term))], mode, out weight, out cancelled);
            }
            finally
            {
                t_combiningFractions = false;
            }
            bool simpler = (DenominatorWeight(after) < weight || cancelled) &&
                NodeCount(after) < NodeCount(BuildSum([.. component.Select(i => terms[i])], Rational.Zero));
            if (!simpler)
                continue;

            foreach (int i in component)
                terms[i] = (Rational.Zero, terms[i].Term);
            foreach (var (coefficient, term) in CollectTerms(after, term => term))
            {
                if (term is Constant c)
                    constant += coefficient * c.Value;
                else
                    terms.Add((coefficient, term));
            }
        }
        terms.RemoveAll(t => t.Coefficient.IsZero);
    }

    // Numbers in denominators, sqrt(3) and pi included, are coefficients and link nothing.
    private static bool HasDenominator(Expr term) => term switch
    {
        Divide(_, var d) => HasVariable(d),
        Power(var b, Constant { Value.Sign: < 0 }) => HasVariable(b),
        Power(Divide(_, var d), _) => HasVariable(d),
        Multiply(var l, var r) => HasDenominator(l) || HasDenominator(r),
        Negate(var a) => HasDenominator(a),
        _ => false,
    };

    private static bool HasVariable(Expr e)
    {
        if (e is Variable)
            return true;
        foreach (Expr child in e.Children)
            if (HasVariable(child))
                return true;
        return false;
    }

    // Groups of two or more terms with a base in their denominators, or one with a base below
    // that another has above in a root: sqrt(r) - x^2/sqrt(r). x + 1/x isn't put together, as
    // nothing could cancel.
    private static IEnumerable<List<int>> LinkedByDenominators(List<(Rational Coefficient, Expr Term)> terms)
    {
        var below = new HashSet<Expr>();
        var rootsBelow = new HashSet<Expr>();
        foreach (var (_, term) in terms)
            if (HasDenominator(term))
                VisitBases(term, false, 1, (b, k, isBelow) =>
                {
                    if (isBelow && HasVariable(b))
                    {
                        below.Add(b);
                        if (!k.IsInteger)
                            rootsBelow.Add(b);
                    }
                });

        if (below.Count == 0)
            return [];

        var root = Enumerable.Range(0, terms.Count).ToArray();
        int Find(int i) => root[i] == i ? i : root[i] = Find(root[i]);
        var first = new Dictionary<Expr, int>();
        bool linked = false;
        for (int i = 0; i < terms.Count; i++)
        {
            int index = i;
            VisitBases(terms[i].Term, false, 1, (b, k, isBelow) =>
            {
                if (!below.Contains(b) || !isBelow && k.IsInteger && !rootsBelow.Contains(b))
                    return;
                if (first.TryGetValue(b, out int j))
                {
                    root[Find(index)] = Find(j);
                    linked = true;
                }
                else
                    first[b] = index;
            });
        }

        return linked ? Enumerable.Range(0, terms.Count).GroupBy(Find).Select(g => g.ToList()).Where(g => g.Count > 1) : [];
    }

    private const int MaxFractionDepth = 3;

    // (sum of c N L/D) / L, with L the product of the highest power of every base below, its
    // total power, and whether terms of the numerator cancelled. The numerator, multiplied out, is
    // put over its own common denominator in turn: -sqrt(r) + 3x^2/sqrt(r) + ... has one.
    private static Expr OverCommonDenominator(
        List<FractionParts> parts, SimplifyMode mode, out Rational weight, out bool cancelled, int depth = 0)
    {
        Expr numerator = CommonNumerator(parts, mode, out Expr denominator, out weight, out cancelled, depth);
        return new Divide(numerator, denominator).Simplify(mode);
    }

    private static Expr CommonNumerator(
        List<FractionParts> parts, SimplifyMode mode, out Expr denominator, out Rational weight, out bool cancelled, int depth = 0)
    {
        var common = new Dictionary<Expr, Rational>();
        foreach (FractionParts p in parts)
            foreach (var (b, k) in p.Bottom)
                common[b] = common.TryGetValue(b, out Rational m) && m >= k ? m : k;
        weight = Weight(common);

        // Each numerator is multiplied out on its own, so that terms that cancel in the sum show.
        Expr? numerator = null;
        int separate = 0;
        foreach (FractionParts p in parts)
        {
            var factors = new Dictionary<Expr, Rational>(p.Top);
            foreach (var (b, k) in common)
            {
                Rational missing = k - p.Bottom.GetValueOrDefault(b);
                if (!missing.IsZero)
                    factors[b] = factors.GetValueOrDefault(b) + missing;
            }
            Expr term = PowerProduct(p.Coefficient, factors).Expand(mode);
            separate += TermCount(term);
            numerator = numerator is null ? term : new Add(numerator, term);
        }

        Expr expanded = numerator!.Simplify(mode);
        cancelled = TermCount(expanded) < separate;
        if (depth < MaxFractionDepth && CollectTerms(expanded, term => term) is { Count: > 1 } inner && inner.Any(t => HasDenominator(t.Term)))
        {
            expanded = OverCommonDenominator([.. inner.Select(t => FractionParts.Of(t.Coefficient, t.Term))], mode, out _, out bool innerCancelled, depth + 1);
            cancelled |= innerCancelled;
        }
        denominator = PowerProduct(Rational.One, common);
        return expanded;
    }

    // n / (S * rest) with S a sum with fractions in it, S = N/L: n L / (N * rest), and so for S^k.
    private static bool TryClearFractionsBelow(Expr n, Expr d, Assumptions assumptions, SimplifyMode mode, out Expr? result)
    {
        result = null;
        if (SumWithFractions(d) is not { } sum)
            return false;

        Expr numerator, common;
        bool combining = t_combiningFractions;
        t_combiningFractions = true;
        try
        {
            numerator = CommonNumerator(
                [.. CollectTerms(sum, term => term).Select(t => FractionParts.Of(t.Coefficient, t.Term))], mode, out common, out _, out _);
        }
        finally
        {
            t_combiningFractions = combining;
        }
        if (numerator is Constant { Value.IsZero: true } || HasDenominator(numerator) || HasFractionTerm(numerator) ||
            !NonZeroForDomain(common, assumptions, mode))
            return false;

        int power = 1;
        Expr below = Replaced(d, sum, numerator, ref power);
        if (ReferenceEquals(below, d))
            return false;
        result = new Divide(new Multiply(n, power == 1 ? common : new Power(common, new Constant(power))), below);
        return true;
    }

    // d with the sum in it replaced by its numerator, and the power the sum is raised to.
    private static Expr Replaced(Expr e, Expr sum, Expr numerator, ref int power)
    {
        switch (e)
        {
            case var _ when ReferenceEquals(e, sum):
                return numerator;
            case Power(var b, Constant k) when ReferenceEquals(b, sum):
                power = (int)k.Value.Numerator;
                return new Power(numerator, k);
            case Multiply(var l, var r):
                Expr left = Replaced(l, sum, numerator, ref power);
                if (!ReferenceEquals(left, l))
                    return new Multiply(left, r);
                Expr right = Replaced(r, sum, numerator, ref power);
                return ReferenceEquals(right, r) ? e : new Multiply(l, right);
            case Negate(var a):
                Expr inner = Replaced(a, sum, numerator, ref power);
                return ReferenceEquals(inner, a) ? e : new Negate(inner);
            default:
                return e;
        }
    }

    // The first sum with a fraction among the factors of d, also under a positive integer power.
    private static Expr? SumWithFractions(Expr d) => d switch
    {
        Add or Subtract => HasFractionTerm(d) ? d : null,
        Multiply(var l, var r) => SumWithFractions(l) ?? SumWithFractions(r),
        Negate(var a) => SumWithFractions(a),
        Power(var b and (Add or Subtract), Constant { Value: { IsInteger: true, Sign: > 0 } k })
            when k.Numerator <= MaxSplitPower => SumWithFractions(b),
        _ => null,
    };

    private static bool HasFractionTerm(Expr sum) => sum switch
    {
        Add(var l, var r) => HasFractionTerm(l) || HasFractionTerm(r),
        Subtract(var l, var r) => HasFractionTerm(l) || HasFractionTerm(r),
        _ => HasDenominator(sum),
    };

    private static int TermCount(Expr e) => e is Constant { Value.IsZero: true } ? 0 : CollectTerms(e, term => term).Count;

    private static Rational Weight(Dictionary<Expr, Rational> bases) =>
        bases.Where(b => HasVariable(b.Key)).Aggregate(Rational.Zero, (sum, b) => sum + b.Value);

    // The highest total power below in the terms of e: 0 once everything has cancelled.
    private static Rational DenominatorWeight(Expr e) =>
        CollectTerms(e, term => term).Select(t => Weight(FractionParts.Of(t.Coefficient, t.Term).Bottom)).Max();

    private static Expr PowerProduct(Rational coefficient, Dictionary<Expr, Rational> factors)
    {
        Expr result = new Constant(coefficient);
        foreach (var (b, k) in factors)
            result = new Multiply(result, k.IsOne ? b : k == Half ? new Sqrt(b) : new Power(b, new Constant(k)));
        return result;
    }

    private const int MaxSplitPower = 64;

    // Calls found(b, k, below) for each factor b^k of a term, sqrt(b) counted as b^(1/2), and
    // returns the number the term holds. Integer powers of products and quotients are taken
    // apart: (S * z^2)^2 = S^2 z^4, which shares the base S with (S * z)^2.
    private static Rational VisitBases(Expr e, bool below, int power, Action<Expr, Rational, bool> found)
    {
        switch (e)
        {
            case Multiply(var l, var r):
                return VisitBases(l, below, power, found) * VisitBases(r, below, power, found);
            case Divide(var n, var d):
                return VisitBases(n, below, power, found) * VisitBases(d, !below, power, found);
            case Negate(var a):
                Rational inner = VisitBases(a, below, power, found);
                return power % 2 != 0 ? -inner : inner;
            case Constant c when !c.Value.IsZero:
                Rational value = c.Value.Pow(power);
                return below ? Rational.One / value : value;
            case Power(var b, Constant { Value: { IsInteger: true } k }) when b is not Constant && power * System.Numerics.BigInteger.Abs(k.Numerator) <= MaxSplitPower:
                return VisitBases(b, k.Sign < 0 ? !below : below, power * (int)System.Numerics.BigInteger.Abs(k.Numerator), found);
            case Power(var b, Constant { Value: var k }) when b is not Constant:
                found(b, k.Abs() * power, k.Sign < 0 ? !below : below);
                return Rational.One;
            case Sqrt(var b) when b is not Constant:
                found(b, Half * power, below);
                return Rational.One;
            default:
                found(e, power, below);
                return Rational.One;
        }
    }

    // A term as c * (product of b^k) / (product of b^k).
    private sealed class FractionParts
    {
        public Rational Coefficient { get; private set; }
        public Dictionary<Expr, Rational> Top { get; } = [];
        public Dictionary<Expr, Rational> Bottom { get; } = [];

        public static FractionParts Of(Rational coefficient, Expr term)
        {
            var parts = new FractionParts();
            parts.Coefficient = coefficient * VisitBases(term, false, 1, (b, k, below) =>
            {
                var side = below ? parts.Bottom : parts.Top;
                side[b] = side.GetValueOrDefault(b) + k;
            });

            // R / R^(3/2) = 1 / sqrt(R): a base that stays below keeps the domain as it was.
            foreach (Expr b in parts.Top.Keys.Where(parts.Bottom.ContainsKey).ToList())
                if (parts.Bottom[b] > parts.Top[b])
                {
                    parts.Bottom[b] -= parts.Top[b];
                    parts.Top.Remove(b);
                }
            return parts;
        }
    }

    // Terms equal but for the sign of the sums in them add up: 2(a - b) + 2(b - a) = 0,
    // (a - b)^2 - (b - a)^2 = 0, x/(a - b) + x/(b - a) = 0. A term without a partner keeps its form.
    private static void CollectNegatedSums(List<(Rational Coefficient, Expr Term)> terms)
    {
        // A hash first, the same for S and -S: the full forms are built only where hashes meet.
        Dictionary<int, List<int>>? hashes = null;
        for (int i = 0; i < terms.Count; i++)
        {
            if (!HasSumFactor(terms[i].Term))
                continue;
            hashes ??= [];
            int hash = SignFreeHash(terms[i].Term);
            if (!hashes.TryGetValue(hash, out var same))
                hashes[hash] = same = [];
            same.Add(i);
        }
        if (hashes is null || hashes.Count == hashes.Values.Sum(same => same.Count))
            return;

        var groups = new Dictionary<Expr, List<(int Index, int Sign)>>();
        foreach (int i in hashes.Values.Where(same => same.Count > 1).SelectMany(same => same))
        {
            var (key, sign) = WithPositiveSums(terms[i].Term);
            if (!groups.TryGetValue(key, out var members))
                groups[key] = members = [];
            members.Add((i, sign));
        }

        var removed = new HashSet<int>();
        foreach (var members in groups.Values.Where(members => members.Count > 1))
        {
            Rational total = members.Aggregate(Rational.Zero, (sum, m) => sum + terms[m.Index].Coefficient * m.Sign);
            // The form that keeps a positive coefficient: 3(1 - x) + 2(x - 1) = 1 - x, not -(x - 1).
            var (kept, keptSign) = members.FirstOrDefault(m => (total * m.Sign).Sign > 0, members[0]);
            terms[kept] = (total * keptSign, terms[kept].Term);
            removed.UnionWith(members.Where(m => m.Index != kept).Select(m => m.Index));
        }
        for (int i = terms.Count - 1; i >= 0; i--)
            if (removed.Contains(i))
                terms.RemoveAt(i);
    }

    private static int SignFreeHash(Expr e) => e switch
    {
        Multiply(var l, var r) => unchecked(SignFreeHash(l) + SignFreeHash(r)),
        Divide(var n, var d) => HashCode.Combine(SignFreeHash(n), SignFreeHash(d)),
        Negate(var a) => SignFreeHash(a),
        Add or Subtract => SumHash(e),
        Power(var b and (Add or Subtract), var k) => HashCode.Combine(SumHash(b), k),
        _ => e.GetHashCode(),
    };

    private static int SumHash(Expr sum)
    {
        int hash = 0;
        var pending = new Stack<Expr>();
        pending.Push(sum);
        while (pending.TryPop(out Expr? e))
        {
            switch (e)
            {
                case Add(var l, var r):
                    pending.Push(l);
                    pending.Push(r);
                    break;
                case Subtract(var l, var r):
                    pending.Push(l);
                    pending.Push(r);
                    break;
                default:
                    var (coefficient, term) = ExtractCoefficient(e);
                    hash = unchecked(hash + (term is Constant c
                        ? (coefficient * c.Value).Abs().GetHashCode()
                        : HashCode.Combine(term, coefficient.Abs())));
                    break;
            }
        }
        return hash;
    }

    private static bool HasSumFactor(Expr term) => term switch
    {
        Add or Subtract => true,
        Multiply(var l, var r) => HasSumFactor(l) || HasSumFactor(r),
        Divide(var n, var d) => HasSumFactor(n) || HasSumFactor(d),
        Negate(var a) => HasSumFactor(a),
        Power(Add or Subtract, Constant { Value.IsInteger: true }) => true,
        _ => false,
    };

    // The term with every sum in it written so that S and -S give the same sum, and the sign
    // that took: (b - a) * x -> ((a - b) * x, -1) or ((b - a) * x, 1), the same for both.
    private static (Expr Key, int Sign) WithPositiveSums(Expr term)
    {
        int sign = 1;
        Expr Visit(Expr e)
        {
            switch (e)
            {
                case Multiply(var l, var r):
                    return new Multiply(Visit(l), Visit(r));
                case Divide(var n, var d):
                    return new Divide(Visit(n), Visit(d));
                case Negate(var a):
                    sign = -sign;
                    return Visit(a);
                case Add or Subtract:
                    var (sum, flipped) = PositiveSum(e);
                    if (flipped)
                        sign = -sign;
                    return sum;
                case Power(var b and (Add or Subtract), Constant { Value.IsInteger: true } k):
                    var (powered, flippedBase) = PositiveSum(b);
                    if (flippedBase && !k.Value.Numerator.IsEven)
                        sign = -sign;
                    return new Power(powered, k);
                default:
                    return e;
            }
        }

        Expr key = Visit(term).Canonicalize();
        return (key, sign);
    }

    // The sum with its terms in structural order, negated if the first has a minus sign.
    private static (Expr Sum, bool Flipped) PositiveSum(Expr sum)
    {
        var parts = CollectTerms(sum, term => term)
            .Select(p => p.Term is Constant c ? (p.Coefficient * c.Value, (Expr)new Constant(1)) : p)
            .ToList();
        parts.Sort((a, b) => Canonicalizer.StructuralCompare(a.Item2, b.Item2));
        bool flip = parts[0].Item1.Sign < 0;

        Rational constant = Rational.Zero;
        var rest = new List<(Rational, Expr)>();
        foreach (var (coefficient, term) in parts)
        {
            Rational signed = flip ? -coefficient : coefficient;
            if (term is Constant)
                constant += signed;
            else
                rest.Add((signed, term));
        }
        return (BuildSum(rest, constant), flip);
    }

    // Terms that are c sqrt(d) times the same rest are added up, with the number taken from
    // anywhere in the term: (1/6) pi sqrt(3) + pi/(6sqrt(3)) = (2/9) pi sqrt(3), x/(2y) + x/(3y)
    // = (5/6) x/y. A term without a partner keeps its form, and so do different d: pi + sqrt(2) pi.
    private static void CollectNumericMultiples(List<(Rational Coefficient, Expr Term)> terms)
    {
        var parts = new (Expr Part, Rational Coefficient, System.Numerics.BigInteger Radicand)[terms.Count];
        var groups = new Dictionary<(Expr, System.Numerics.BigInteger), List<int>>();
        bool merges = false;
        for (int i = 0; i < terms.Count; i++)
        {
            var (coefficient, term) = terms[i];
            parts[i] = NumericFactor(term) is var (part, c, d) ? (part, coefficient * c, d) : (term, coefficient, 1);
            var key = (parts[i].Part, parts[i].Radicand);
            if (!groups.TryGetValue(key, out List<int>? members))
                groups[key] = members = [];
            members.Add(i);
            merges |= members.Count > 1;
        }
        if (!merges)
            return;

        var removed = new HashSet<int>();
        foreach (List<int> members in groups.Values.Where(members => members.Count > 1))
        {
            var (rest, _, d) = parts[members[0]];
            Rational total = members.Aggregate(Rational.Zero, (sum, i) => sum + parts[i].Coefficient);
            Expr term = d.IsOne ? rest : rest is Constant { Value.IsOne: true }
                ? new Sqrt(new Constant(new Rational(d)))
                : new Multiply(new Sqrt(new Constant(new Rational(d))), rest);
            terms[members[0]] = (total, term);
            removed.UnionWith(members.Skip(1));
        }
        for (int i = terms.Count - 1; i >= 0; i--)
            if (removed.Contains(i))
                terms.RemoveAt(i);
    }

    // A product or quotient as c sqrt(d) times the rest; null when it holds no number.
    private static (Expr Part, Rational Coefficient, System.Numerics.BigInteger Radicand)? NumericFactor(Expr term)
    {
        var top = new List<Expr>();
        var bottom = new List<Expr>();
        var numbers = new List<Expr>();
        var divisors = new List<Expr>();
        bool negative = false;

        void Split(Expr e, bool below)
        {
            switch (e)
            {
                case Multiply(var l, var r):
                    Split(l, below);
                    Split(r, below);
                    break;
                case Divide(var n, var d):
                    Split(n, below);
                    Split(d, !below);
                    break;
                case Negate(var a):
                    negative = !negative;
                    Split(a, below);
                    break;
                case Constant or Sqrt(Constant { Value.Sign: > 0 }):
                    (below ? divisors : numbers).Add(e);
                    break;
                default:
                    (below ? bottom : top).Add(e);
                    break;
            }
        }

        if (term is not (Multiply or Divide or Negate))
            return null;
        Split(term, false);
        if (numbers.Count == 0 && divisors.Count == 0)
            return null;

        static Expr Product(List<Expr> factors) =>
            factors.Count == 0 ? new Constant(1) : factors.Aggregate((product, factor) => new Multiply(product, factor));

        if (!ExactValues.TryGetSurd(new Divide(Product(numbers), Product(divisors)), out Rational c, out var radicand))
            return null;

        Expr rest = bottom.Count == 0 ? Product(top) : new Divide(Product(top), Product(bottom));
        return (rest.Canonicalize(), negative ? -c : c, radicand);
    }

    // s with c = s^2 r, from the square factors of the numerator and the denominator; null if 1.
    private static Rational? SquareFactor(Rational c)
    {
        if (c.IsZero)
            return null;
        var (top, _) = ExactValues.ExtractPowers(System.Numerics.BigInteger.Abs(c.Numerator), 2);
        var (bottom, _) = ExactValues.ExtractPowers(c.Denominator, 2);
        return top.IsOne && bottom.IsOne ? null : new Rational(top, bottom);
    }

    private static bool TryTakeSquaresOut(Expr radicand, Assumptions assumptions, out Expr? result)
    {
        result = null;
        if (radicand is not (Multiply or Power))
            return false;

        var factors = new List<Expr>();
        Rational coefficient = Rational.One;
        FlattenProduct(radicand, factors, ref coefficient);

        var outside = new List<Expr>();
        Expr rest = new Constant(coefficient);
        foreach (Expr factor in factors)
        {
            if (factor is Power(var b, Constant { Value: { IsInteger: true } k }) && k.Numerator.IsEven && !k.IsZero)
            {
                var half = new Constant(k / 2);
                outside.Add(k.Numerator / 2 % 2 == 0 ? new Power(b, half) : new Power(new Abs(b), half));
            }
            else
                rest = new Multiply(rest, factor);
        }

        if (outside.Count == 0 || !rest.IsProvablyNonNegative(assumptions))
            return false;

        result = outside.Aggregate(new Sqrt(rest) as Expr, (product, factor) => new Multiply(product, factor));
        return true;
    }

    // Higher powers aren't looked for in ln(r^k), nor merged into one logarithm.
    private const int MaxLogarithmPower = 64;
    private const int MaxMergedLogarithmPower = 16;

    private static Expr? LogarithmOfRational(Rational q)
    {
        if (q.IsOne)
            return null;
        if (q.Numerator.IsOne)
            return new Negate(new Ln(new Constant(new Rational(q.Denominator))));

        long bits = Math.Max(q.Numerator.GetBitLength(), q.Denominator.GetBitLength());
        for (int k = (int)Math.Min(MaxLogarithmPower, bits); k >= 2; k--)
            if (TryExactRoot(q, k, out Rational root))
                return new Multiply(new Constant(k), new Ln(new Constant(root)));
        return null;
    }

    // a ln(p) + b ln(q) = a ln(p q^(b/a)) for numbers p, q > 0 where b is a multiple of a:
    // (1/2) ln(1/2) - (1/2) ln(1/3) = (1/2) ln(3/2), ln(6) - ln(2) - ln(3) = 0.
    private static void MergeLogarithms(List<(Rational Coefficient, Expr Term)> terms)
    {
        var logs = new List<int>();
        for (int i = 0; i < terms.Count; i++)
            if (terms[i].Term is Ln(Constant { Value.Sign: > 0 }))
                logs.Add(i);
        if (logs.Count < 2)
            return;

        Rational unit = logs.Select(i => terms[i].Coefficient.Abs()).Min();
        Rational product = Rational.One;
        foreach (int i in logs)
        {
            Rational ratio = terms[i].Coefficient / unit;
            if (!ratio.IsInteger || ratio.Abs() > MaxMergedLogarithmPower)
                return;
            product *= ((Constant)((Ln)terms[i].Term).Argument).Value.Pow((int)ratio.Numerator);
        }

        for (int j = logs.Count - 1; j >= 1; j--)
            terms.RemoveAt(logs[j]);
        if (product.IsOne)
            terms.RemoveAt(logs[0]);
        else
            terms[logs[0]] = (unit, new Ln(new Constant(product)));
    }

    // ln(A) and ln(B) of sums of numbers times powers of e with A = e^k B add up as
    // ln(e^m N) = m + ln(N), N with the lowest power e^0: ln(exp(-1) + 1) - ln(e + 1) = -1.
    // A logarithm without a partner keeps its form.
    private static void MergeExponentialLogarithms(List<(Rational Coefficient, Expr Term)> terms, ref Rational constant)
    {
        List<int>? candidates = null;
        for (int i = 0; i < terms.Count; i++)
            if (terms[i].Term is Ln(Add or Subtract))
                (candidates ??= []).Add(i);
        if (candidates is not { Count: > 1 })
            return;

        var groups = new Dictionary<Expr, List<(int Index, Rational Shift)>>();
        foreach (int i in candidates)
            if (ExponentialSum(((Ln)terms[i].Term).Argument) is var (normal, shift))
            {
                if (!groups.TryGetValue(normal, out var members))
                    groups[normal] = members = [];
                members.Add((i, shift));
            }

        var removed = new HashSet<int>();
        foreach (var (normal, members) in groups)
        {
            if (members.Count < 2)
                continue;
            Rational total = Rational.Zero;
            Expr log = new Ln(normal);
            foreach (var (index, shift) in members)
            {
                total += terms[index].Coefficient;
                constant += terms[index].Coefficient * shift;
                if (shift.IsZero)
                    log = terms[index].Term;
            }
            terms[members[0].Index] = (total, log);
            removed.UnionWith(members.Skip(1).Select(m => m.Index));
            if (total.IsZero)
                removed.Add(members[0].Index);
        }
        for (int i = terms.Count - 1; i >= 0; i--)
            if (removed.Contains(i))
                terms.RemoveAt(i);
    }

    // A sum of numbers times e^q for rational q as e^m N, N with e^0 as its lowest power.
    private static (Expr Normal, Rational Shift)? ExponentialSum(Expr sum)
    {
        var powers = new SortedDictionary<Rational, Rational>();
        foreach (var (coefficient, term) in CollectTerms(sum, term => term))
        {
            Rational exponent;
            Rational factor = coefficient;
            if (term is Constant c)
                (exponent, factor) = (Rational.Zero, coefficient * c.Value);
            else if (ExponentOfE(term) is Constant q)
                exponent = q.Value;
            else
                return null;
            powers[exponent] = powers.GetValueOrDefault(exponent) + factor;
        }

        var nonzero = powers.Where(p => !p.Value.IsZero).ToList();
        if (nonzero.Count < 2)
            return null;
        Rational lowest = nonzero[0].Key;
        var parts = nonzero.Skip(1).Select(p => (p.Value, (Expr)new Exp(new Constant(p.Key - lowest)))).ToList();
        return (BuildSum(parts, nonzero[0].Value), lowest);
    }

    // A sum of numbers a + b sqrt(d) as one, term by term: (sqrt(2) - 2)/2 + 1 = sqrt(2)/2.
    private static QuadraticSurd? SurdSum(List<(Rational Coefficient, Expr Term)> terms, Rational constant)
    {
        QuadraticSurd? total = new QuadraticSurd(constant, 0, 1);
        foreach (var (coefficient, term) in terms)
        {
            if (total is not { } sum || term.GetVariables().Count > 0 || QuadraticSurd.Of(term) is not { } value)
                return null;
            total = sum + value.Times(coefficient);
        }
        return total;
    }

    // Builds c1*t1 + c2*t2 + ... + constant in the given order, writing negative coefficients as
    // subtraction: x - 2y + 3. The coefficients must be nonzero. Also used by Expand.
    internal static Expr BuildSum(IReadOnlyList<(Rational Coefficient, Expr Term)> terms, Rational constant)
    {
        static Expr Rebuild(Rational coef, Expr term) =>
            coef.IsOne ? term :
            coef.Equals(Rational.MinusOne) ? new Negate(term) :
            new Multiply(new Constant(coef), term);

        if (terms.Count == 0)
            return new Constant(constant);

        Expr result = Rebuild(terms[0].Coefficient, terms[0].Term);
        for (int i = 1; i < terms.Count; i++)
        {
            var (coef, term) = terms[i];
            result = coef.Sign < 0
                ? new Subtract(result, Rebuild(-coef, term))
                : new Add(result, Rebuild(coef, term));
        }

        if (!constant.IsZero)
        {
            result = constant.Sign < 0
                ? new Subtract(result, new Constant(-constant))
                : new Add(result, new Constant(constant));
        }

        return result;
    }

    private static void CollectFactors(Expr expr, Dictionary<Expr, Rational> factors, ref Rational coefficient)
    {
        switch (expr)
        {
            case Multiply(var l, var r):
                CollectFactors(l, factors, ref coefficient);
                CollectFactors(r, factors, ref coefficient);
                break;

            case Negate(var inner):
                coefficient = -coefficient;
                CollectFactors(inner, factors, ref coefficient);
                break;

            case Constant c:
                coefficient *= c.Value;
                break;

            case Power(var b, Constant e) when e.Value.IsInteger:
                AddExponent(factors, b, e.Value);
                break;

            default:
                AddExponent(factors, expr, Rational.One);
                break;
        }
    }

    private static void AddExponent(Dictionary<Expr, Rational> factors, Expr baseExpr, Rational exponent)
    {
        factors[baseExpr] = factors.TryGetValue(baseExpr, out Rational existing)
            ? existing + exponent
            : exponent;
    }

    private static (Rational Coefficient, Dictionary<Expr, Rational> Factors) ExtractFactors(Expr expr)
    {
        var factors = new Dictionary<Expr, Rational>();
        Rational coefficient = Rational.One;
        CollectFactors(expr, factors, ref coefficient);
        return (coefficient, factors);
    }

    private static bool IsConstantOne(Expr e) => e is Constant c && c.Value.IsOne;

    private static Expr BuildProduct(Rational coefficient, Dictionary<Expr, Rational> factors)
    {
        Expr? result = coefficient.IsOne ? null : new Constant(coefficient);

        foreach (var (baseExpr, exponent) in factors)
        {
            Expr term = exponent.IsOne ? baseExpr : new Power(baseExpr, new Constant(exponent));
            result = result is null ? term : new Multiply(result, term);
        }

        return result ?? new Constant(coefficient); // everything cancelled — pure coefficient (often 1)
    }

    // The constant factors of a quotient are reduced like a fraction, with the sign in the
    // numerator: 2x/2 = x, 4x/6 = 2x/3, x/(-2) = -x/2, (2y)/(4x) = y/(2x). Both sides are
    // multiplied by the same nonzero number, so the domain stays the same.
    private static bool TryReduceConstantFactors(
        Expr numerator, Expr denominator, Assumptions assumptions, SimplifyMode mode, Dictionary<Expr, Expr> cache, out Expr? result)
    {
        result = null;

        // 0/d and division by 0 are left to their own rules.
        Rational top = ConstantFactor(numerator), bottom = ConstantFactor(denominator);
        if (top.IsZero || bottom.IsZero)
            return false;

        Rational ratio = top / bottom;
        if (top == new Rational(ratio.Numerator) && bottom == new Rational(ratio.Denominator))
            return false; // already in lowest terms

        var scale = new Constant(new Rational(ratio.Denominator) / bottom);
        result = SimplifyCached(
            new Divide(new Multiply(scale, numerator), new Multiply(scale, denominator)), assumptions, mode, cache);
        return true;
    }

    // The product of the constant factors of a product: 6 for -2x * (-3y), 1 for anything else.
    private static Rational ConstantFactor(Expr expr) => expr switch
    {
        Constant c => c.Value,
        Negate(var a) => -ConstantFactor(a),
        Multiply(var l, var r) => ConstantFactor(l) * ConstantFactor(r),
        _ => Rational.One
    };

    // (x^2 - 1)/(x - 1) = x + 1, (x - 1)/(x^2 - 1) = 1/(x + 1): a sum on one side that divides a
    // multiplied-out factor of the other side, or the whole of it, as a polynomial cancels. Like
    // x/x = 1 this gains the zeros of the sum, so Strict mode needs it provably nonzero.
    private static bool TryCancelPolynomialFactors(Expr n, Expr d, Assumptions assumptions, SimplifyMode mode, out Expr? result)
    {
        result = null;
        if (!HasSumFactor(n) && !HasSumFactor(d))
            return false;

        var (top, topFactors) = ExtractFactors(n);
        var (bottom, bottomFactors) = ExtractFactors(d);
        var polynomials = new AtomPolynomials();
        if (!TryDivideBySum(polynomials, ref top, topFactors, bottomFactors, assumptions, mode) &&
            !TryDivideBySum(polynomials, ref bottom, bottomFactors, topFactors, assumptions, mode))
            return false;

        Expr numerator = BuildProduct(top, topFactors), denominator = BuildProduct(bottom, bottomFactors);
        result = IsConstantOne(denominator) ? numerator : new Divide(numerator, denominator);
        return true;
    }

    // Divides one factor of a side, or the side as a whole, by a sum among the factors of the
    // other side, and updates both.
    private static bool TryDivideBySum(AtomPolynomials polynomials, ref Rational coefficient, Dictionary<Expr, Rational> side,
        Dictionary<Expr, Rational> other, Assumptions assumptions, SimplifyMode mode)
    {
        foreach (var (sum, exponent) in other)
        {
            if (sum is not (Add or Subtract) || !exponent.IsInteger || exponent.Sign <= 0 ||
                polynomials.Of(sum) is not { Count: > 1 } divisor || !NonZeroForDomain(sum, assumptions, mode))
                continue;

            var degrees = polynomials.Degrees(divisor).ToList();
            bool Reaches(Expr dividend) => degrees.All(d => AtomPolynomials.Degree(dividend, d.Atom) >= d.Degree);

            foreach (var (factor, power) in side)
                if (factor is Add or Subtract && power.IsInteger && power.Sign > 0 && AtomPolynomials.IsExpanded(factor) && Reaches(factor) &&
                    polynomials.Of(factor) is { } dividend && AtomPolynomials.ExactQuotient(dividend, divisor) is { } quotient)
                {
                    TakeOne(side, factor);
                    TakeOne(other, sum);
                    AddFactor(side, polynomials.ToExpr(quotient), ref coefficient);
                    return true;
                }

            // The whole side is multiplied out: only where its factors are, and that doesn't make
            // it longer.
            Expr product = BuildProduct(coefficient, side);
            if (side.Count > 1 && side.Keys.All(AtomPolynomials.IsExpanded) && Reaches(product) && polynomials.Of(product) is { } whole &&
                AtomPolynomials.ExactQuotient(whole, divisor) is { } wholeQuotient &&
                polynomials.ToExpr(wholeQuotient) is var expanded && NodeCount(expanded) <= NodeCount(product))
            {
                side.Clear();
                coefficient = Rational.One;
                TakeOne(other, sum);
                AddFactor(side, expanded, ref coefficient);
                return true;
            }
        }
        return false;
    }

    private static void TakeOne(Dictionary<Expr, Rational> factors, Expr factor)
    {
        if (factors[factor].IsOne)
            factors.Remove(factor);
        else
            factors[factor] -= Rational.One;
    }

    private static void AddFactor(Dictionary<Expr, Rational> factors, Expr factor, ref Rational coefficient)
    {
        if (factor is Constant c)
            coefficient *= c.Value;
        else
            AddExponent(factors, factor, Rational.One);
    }

    private static bool TryCancelCommonFactors(
        Expr numerator, Expr denominator, Assumptions assumptions, SimplifyMode mode, Dictionary<Expr, Expr> cache, out Expr? result)
    {
        result = null;

        var (numCoefficient, numFactors) = ExtractFactors(numerator);
        var (denCoefficient, denFactors) = ExtractFactors(denominator);

        var cancellable = new List<Expr>();
        foreach (Expr baseExpr in numFactors.Keys)
            if (denFactors.ContainsKey(baseExpr) && NonZeroForDomain(baseExpr, assumptions, mode))
                cancellable.Add(baseExpr);

        if (cancellable.Count == 0)
            return false; // nothing safe to cancel - leave the whole node alone

        foreach (Expr baseExpr in cancellable)
        {
            Rational net = numFactors[baseExpr] - denFactors[baseExpr];
            numFactors.Remove(baseExpr);
            denFactors.Remove(baseExpr);

            if (net.IsZero) continue;
            if (net.Sign > 0) numFactors[baseExpr] = net;
            else denFactors[baseExpr] = -net;
        }

        Expr newNumerator = BuildProduct(numCoefficient, numFactors);
        Expr newDenominator = BuildProduct(denCoefficient, denFactors);

        result = SimplifyCached(
            IsConstantOne(newDenominator) ? newNumerator : new Divide(newNumerator, newDenominator),
            assumptions, mode, cache);

        return true;
    }
}