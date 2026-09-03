using Epsilon;

namespace Example
{
    public class Example
    {
        public static void Main()
        {
            Console.WriteLine("Welcome to the Epsilon Math Library!");

            RunDemo("x^2 + 3*x - 1", 2);
            RunDemo("(x + 1) / (x - 1)", 3);
        }

        private static void RunDemo(string expression, double point)
        {
            try
            {
                Expr f = ExprParser.Parse(expression);
                Expr df = f.Differentiate();

                Console.WriteLine($"f(x)     = {f}");
                Console.WriteLine($"f'(x)    = {df}");
                Console.WriteLine($"f({point})    = {f.Evaluate(point)}");
                Console.WriteLine($"f'({point})   = {df.Evaluate(point)}");
                Console.WriteLine();
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Parse error in \"{expression}\": {ex.Message}\n");
            }
        }
    }
}