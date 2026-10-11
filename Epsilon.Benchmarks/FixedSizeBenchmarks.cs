using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Epsilon.LinearAlgebra;

namespace Epsilon.Benchmarks;

// The fixed-size types of float and double against System.Numerics, which has float only and
// uses SIMD. Each category is one operation, with System.Numerics as the baseline of the ratio.
// A short run shows the ratios well enough: ... -- --filter '*FixedSize*' --job short
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class FixedSizeBenchmarks
{
    private const int PointCount = 1000;

    private System.Numerics.Matrix4x4 _numericsModel, _numericsView;
    private Matrix4x4<float> _floatModel, _floatView;
    private Matrix4x4<double> _doubleModel, _doubleView;

    private readonly System.Numerics.Vector3[] _numericsPoints = new System.Numerics.Vector3[PointCount];
    private readonly System.Numerics.Vector3[] _numericsResults = new System.Numerics.Vector3[PointCount];
    private readonly Vector3<float>[] _floatPoints = new Vector3<float>[PointCount];
    private readonly Vector3<float>[] _floatResults = new Vector3<float>[PointCount];
    private readonly Vector3<double>[] _doublePoints = new Vector3<double>[PointCount];
    private readonly Vector3<double>[] _doubleResults = new Vector3<double>[PointCount];

    [GlobalSetup]
    public void Setup()
    {
        // A model matrix (scale, rotate, move) and a view-like one.
        _doubleModel = Matrix4x4<double>.Translation((1, 2, 3)) * Matrix4x4<double>.Rotation((1, 2, 3), 0.7) * Matrix4x4<double>.Scale((2, 3, 4));
        _doubleView = Matrix4x4<double>.RotationY(1.1) * Matrix4x4<double>.Translation((-5, 0, 2));
        _floatModel = _doubleModel.Map(float.CreateTruncating);
        _floatView = _doubleView.Map(float.CreateTruncating);
        _numericsModel = _floatModel.ToSystemNumerics();
        _numericsView = _floatView.ToSystemNumerics();

        var random = new Random(1);
        for (int i = 0; i < PointCount; i++)
        {
            _doublePoints[i] = (random.NextDouble() * 100, random.NextDouble() * 100, random.NextDouble() * 100);
            _floatPoints[i] = _doublePoints[i].Map(float.CreateTruncating);
            _numericsPoints[i] = _floatPoints[i].ToSystemNumerics();
        }
    }

    // Over many vectors: a single one is hoisted out of the measuring loop by the JIT.
    [Benchmark(Baseline = true), BenchmarkCategory("Normalize1000")]
    public void NormalizeNumerics()
    {
        for (int i = 0; i < PointCount; i++)
            _numericsResults[i] = System.Numerics.Vector3.Normalize(_numericsPoints[i]);
    }

    [Benchmark, BenchmarkCategory("Normalize1000")]
    public void NormalizeFloat()
    {
        for (int i = 0; i < PointCount; i++)
            _floatResults[i] = _floatPoints[i].Normalize();
    }

    [Benchmark, BenchmarkCategory("Normalize1000")]
    public void NormalizeDouble()
    {
        for (int i = 0; i < PointCount; i++)
            _doubleResults[i] = _doublePoints[i].Normalize();
    }

    // view * model here is model * view in System.Numerics: the same map.
    [Benchmark(Baseline = true), BenchmarkCategory("Multiply4x4")]
    public System.Numerics.Matrix4x4 MultiplyNumerics() => _numericsModel * _numericsView;

    [Benchmark, BenchmarkCategory("Multiply4x4")]
    public Matrix4x4<float> MultiplyFloat() => _floatView * _floatModel;

    [Benchmark, BenchmarkCategory("Multiply4x4")]
    public Matrix4x4<double> MultiplyDouble() => _doubleView * _doubleModel;

    [Benchmark(Baseline = true), BenchmarkCategory("Inverse4x4")]
    public System.Numerics.Matrix4x4 InverseNumerics()
    {
        System.Numerics.Matrix4x4.Invert(_numericsModel, out var inverse);
        return inverse;
    }

    [Benchmark, BenchmarkCategory("Inverse4x4")]
    public Matrix4x4<float> InverseFloat()
    {
        _floatModel.TryInverse(out var inverse);
        return inverse;
    }

    [Benchmark, BenchmarkCategory("Inverse4x4")]
    public Matrix4x4<double> InverseDouble()
    {
        _doubleModel.TryInverse(out var inverse);
        return inverse;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("TransformPoints1000")]
    public void TransformPointsNumerics()
    {
        for (int i = 0; i < PointCount; i++)
            _numericsResults[i] = System.Numerics.Vector3.Transform(_numericsPoints[i], _numericsModel);
    }

    [Benchmark, BenchmarkCategory("TransformPoints1000")]
    public void TransformPointsFloat() => _floatModel.TransformPoints(_floatPoints, _floatResults);

    [Benchmark, BenchmarkCategory("TransformPoints1000")]
    public void TransformPointsDouble() => _doubleModel.TransformPoints(_doublePoints, _doubleResults);
}
