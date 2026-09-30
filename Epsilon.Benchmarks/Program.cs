using BenchmarkDotNet.Running;

// Run in Release: dotnet run -c Release --project Epsilon.Benchmarks
// Pick benchmarks with a filter: ... -- --filter '*Simplify*'
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
