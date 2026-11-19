using Epsilon.Core;

namespace Epsilon.Solving;

/// <summary>Exact solutions of equations written as expressions; see <see cref="Equation"/>.</summary>
public static class SolvingExtensions
{
    extension(Expr expr)
    {
        /// <summary>Solves expr = 0 for its only variable; see <see cref="Equation.Solve(string, Assumptions?)"/>.</summary>
        /// <exception cref="InvalidOperationException">The expression has more than one variable.</exception>
        public SolutionSet Solve() => new Equation(expr, 0).Solve();

        /// <summary>Solves expr = 0 for <paramref name="variable"/>; see <see cref="Equation.Solve(string, Assumptions?)"/>.</summary>
        public SolutionSet Solve(string variable, Assumptions? assumptions = null) =>
            new Equation(expr, 0).Solve(variable, assumptions);

        /// <summary>Solves expr = <paramref name="right"/> for their only variable; see <see cref="Equation.Solve(string, Assumptions?)"/>.</summary>
        /// <exception cref="InvalidOperationException">The sides have more than one variable.</exception>
        public SolutionSet Solve(Expr right) => new Equation(expr, right).Solve();

        /// <summary>Solves expr = <paramref name="right"/> for <paramref name="variable"/>; see <see cref="Equation.Solve(string, Assumptions?)"/>.</summary>
        public SolutionSet Solve(Expr right, string variable, Assumptions? assumptions = null) =>
            new Equation(expr, right).Solve(variable, assumptions);

        /// <summary>Solves expr = 0 over the complex numbers for its only variable; see <see cref="Equation.SolveComplex(string)"/>.</summary>
        /// <exception cref="InvalidOperationException">The expression has more than one variable.</exception>
        public SolutionSet SolveComplex() => new Equation(expr, 0).SolveComplex();

        /// <summary>Solves expr = 0 over the complex numbers for <paramref name="variable"/>; see <see cref="Equation.SolveComplex(string)"/>.</summary>
        public SolutionSet SolveComplex(string variable) => new Equation(expr, 0).SolveComplex(variable);

        /// <summary>Solves expr = <paramref name="right"/> over the complex numbers for their only variable; see <see cref="Equation.SolveComplex(string)"/>.</summary>
        /// <exception cref="InvalidOperationException">The sides have more than one variable.</exception>
        public SolutionSet SolveComplex(Expr right) => new Equation(expr, right).SolveComplex();

        /// <summary>Solves expr = <paramref name="right"/> over the complex numbers for <paramref name="variable"/>; see <see cref="Equation.SolveComplex(string)"/>.</summary>
        public SolutionSet SolveComplex(Expr right, string variable) => new Equation(expr, right).SolveComplex(variable);
    }
}
