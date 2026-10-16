using System.Numerics;
using Xunit;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.LinearAlgebra;

// "Vector3<T> and Matrix4x4<T> don't clash with Vector3 and Matrix4x4 of System.Numerics" (the
// README): with both namespaces imported, the names without a type argument are System.Numerics'.
public class LinearAlgebraNumericsNamesTests
{
    [Fact]
    public void Fixed_size_names_do_not_clash_with_System_Numerics()
    {
        Vector3 numerics = new(1, 2, 3);
        Vector3<float> ours = (1, 2, 3);
        Matrix4x4 numericsMatrix = Matrix4x4.CreateTranslation(1, 2, 3);
        Matrix4x4<float> ourMatrix = Matrix4x4<float>.FromSystemNumerics(numericsMatrix);

        Assert.Equal(numerics, ours.ToSystemNumerics());
        Assert.Equal(Vector3.Transform(numerics, numericsMatrix), ourMatrix.TransformPoint(ours).ToSystemNumerics());
    }
}
