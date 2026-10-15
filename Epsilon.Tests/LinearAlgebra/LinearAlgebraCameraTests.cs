using Xunit;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.LinearAlgebra;

public class LookAtTests
{
    private static readonly Vector3<double> Eye = (1, 2, 5), Target = (0, 0, 0), Up = Vector3<double>.UnitY;

    [Fact]
    public void The_camera_ends_up_at_the_origin_looking_along_minus_z()
    {
        var view = Matrix4x4<double>.LookAt(Eye, Target, Up);
        double distance = Eye.Distance(Target);

        Assert.True(view.TransformPoint(Eye).IsApproximately(Vector3<double>.Zero, absoluteTolerance: 1e-15));
        Assert.True(view.TransformPoint(Target).IsApproximately((0, 0, -distance)));
        Assert.True(view.TransformDirection(Up).Y > 0);   // up stays up on the screen

        // A point to the right of the line of view: x > 0.
        Vector3<double> right = (Target - Eye).Cross(Up);
        Assert.True(view.TransformPoint(Target + right).X > 0);

        // Only a turn and a move: the corner is a rotation.
        Assert.True(view.TryDecompose(out _, out _, out var scale));
        Assert.True(scale.IsApproximately(Vector3<double>.One));
    }

    [Fact]
    public void Left_handed_looks_along_plus_z()
    {
        var view = Matrix4x4<double>.LookAtLeftHanded(Eye, Target, Up);

        Assert.True(view.TransformPoint(Eye).IsApproximately(Vector3<double>.Zero, absoluteTolerance: 1e-15));
        Assert.True(view.TransformPoint(Target).IsApproximately((0, 0, Eye.Distance(Target))));
        Assert.True(view.TransformDirection(Up).Y > 0);
        Assert.True(view.TransformPoint(Target + (Target - Eye).Cross(Up)).X < 0);   // the same point is now on the left
    }

    [Fact]
    public void Agrees_with_System_Numerics()
    {
        var eye = new System.Numerics.Vector3(1, 2, 5);
        var target = new System.Numerics.Vector3(-0.5f, 0.3f, 0);
        var up = new System.Numerics.Vector3(0.1f, 1, 0);

        Assert.True(Matrix4x4<float>.FromSystemNumerics(System.Numerics.Matrix4x4.CreateLookAt(eye, target, up))
            .IsApproximately(Matrix4x4<float>.LookAt((1, 2, 5), (-0.5f, 0.3f, 0), (0.1f, 1, 0))));
        Assert.True(Matrix4x4<float>.FromSystemNumerics(System.Numerics.Matrix4x4.CreateLookAtLeftHanded(eye, target, up))
            .IsApproximately(Matrix4x4<float>.LookAtLeftHanded((1, 2, 5), (-0.5f, 0.3f, 0), (0.1f, 1, 0))));
    }

    [Fact]
    public void The_direction_of_view_and_up_must_be_defined()
    {
        Assert.Equal("target", Assert.Throws<ArgumentException>(() => Matrix4x4<double>.LookAt(Eye, Eye, Up)).ParamName);
        Assert.Equal("up", Assert.Throws<ArgumentException>(() => Matrix4x4<double>.LookAt((0, 5, 0), Target, Up)).ParamName);
        Assert.Equal("up", Assert.Throws<ArgumentException>(() => Matrix4x4<double>.LookAtLeftHanded(Eye, Target, Vector3<double>.Zero)).ParamName);
    }
}

public class PerspectiveTests
{
    private const double FieldOfView = Math.PI / 3, Aspect = 16.0 / 9, Near = 0.5, Far = 100;

    private static readonly (DepthRange Range, double Near, double Far)[] Ranges =
    [
        (DepthRange.ZeroToOne, 0, 1),
        (DepthRange.MinusOneToOne, -1, 1),
        (DepthRange.OneToZero, 1, 0),
    ];

    [Fact]
    public void The_frustum_maps_to_the_unit_box()
    {
        double halfHeight = Math.Tan(FieldOfView / 2), halfWidth = halfHeight * Aspect;
        foreach (var (range, nearDepth, farDepth) in Ranges)
        {
            foreach (bool rightHanded in new[] { true, false })
            {
                var projection = rightHanded
                    ? Matrix4x4<double>.Perspective(FieldOfView, Aspect, Near, Far, range)
                    : Matrix4x4<double>.PerspectiveLeftHanded(FieldOfView, Aspect, Near, Far, range);
                double ahead = rightHanded ? -1 : 1;   // the sign of z in front of the camera

                Assert.True(projection.TransformPoint((0, 0, ahead * Near)).IsApproximately((0, 0, nearDepth), absoluteTolerance: 1e-15), $"{range} {rightHanded}");
                Assert.True(projection.TransformPoint((0, 0, ahead * Far)).IsApproximately((0, 0, farDepth), absoluteTolerance: 1e-15), $"{range} {rightHanded}");

                // The top right corners of the near and the far plane.
                Assert.True(projection.TransformPoint((halfWidth * Near, halfHeight * Near, ahead * Near)).IsApproximately((1, 1, nearDepth), absoluteTolerance: 1e-15));
                Assert.True(projection.TransformPoint((-halfWidth * Far, -halfHeight * Far, ahead * Far)).IsApproximately((-1, -1, farDepth), absoluteTolerance: 1e-13));
            }
        }
    }

    [Fact]
    public void Perspective_makes_far_things_smaller()
    {
        var projection = Matrix4x4<double>.Perspective(FieldOfView, Aspect, Near, Far);

        double near = projection.TransformPoint((1, 0, -5)).X, far = projection.TransformPoint((1, 0, -10)).X;
        Assert.Equal(near / 2, far, 1e-15);
    }

    [Fact]
    public void The_far_plane_may_be_infinite()
    {
        foreach (var (range, nearDepth, farDepth) in Ranges)
        {
            var projection = Matrix4x4<double>.Perspective(FieldOfView, Aspect, Near, double.PositiveInfinity, range);

            Assert.Equal(nearDepth, projection.TransformPoint((0, 0, -Near)).Z, 1e-15);
            Assert.Equal(farDepth, projection.TransformPoint((0, 0, -1e12)).Z, 1e-11);
            Assert.True(double.IsFinite(projection.M33) && double.IsFinite(projection.M34));
        }

        // Reverse-Z without a far plane: depth = near / distance.
        var reversed = Matrix4x4<double>.Perspective(FieldOfView, Aspect, Near, double.PositiveInfinity, DepthRange.OneToZero);
        Assert.Equal(0, reversed.M33);
        Assert.Equal(Near, reversed.M34);
        Assert.Equal(Near / 40, reversed.TransformPoint((0, 0, -40)).Z, 1e-17);
    }

    [Fact]
    public void Agrees_with_System_Numerics()
    {
        float fov = MathF.PI / 4, aspect = 4f / 3, near = 0.1f, far = 50;

        Assert.True(Matrix4x4<float>.FromSystemNumerics(System.Numerics.Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, near, far))
            .IsApproximately(Matrix4x4<float>.Perspective(fov, aspect, near, far)));
        Assert.True(Matrix4x4<float>.FromSystemNumerics(System.Numerics.Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(fov, aspect, near, far))
            .IsApproximately(Matrix4x4<float>.PerspectiveLeftHanded(fov, aspect, near, far)));
        Assert.True(Matrix4x4<float>.FromSystemNumerics(System.Numerics.Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, near, float.PositiveInfinity))
            .IsApproximately(Matrix4x4<float>.Perspective(fov, aspect, near, float.PositiveInfinity)));
    }

    [Theory]
    [InlineData(0.0, Aspect, Near, Far, "fieldOfViewY")]
    [InlineData(Math.PI, Aspect, Near, Far, "fieldOfViewY")]
    [InlineData(FieldOfView, -1.0, Near, Far, "aspectRatio")]
    [InlineData(FieldOfView, Aspect, 0.0, Far, "near")]
    [InlineData(FieldOfView, Aspect, Near, Near, "far")]
    [InlineData(FieldOfView, Aspect, Near, double.NaN, "far")]
    public void Invalid_arguments(double fov, double aspect, double near, double far, string parameter) =>
        Assert.Equal(parameter, Assert.Throws<ArgumentOutOfRangeException>(() => Matrix4x4<double>.Perspective(fov, aspect, near, far)).ParamName);

    [Fact]
    public void Invalid_depth_range()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Matrix4x4<double>.Perspective(FieldOfView, Aspect, Near, Far, (DepthRange)7));
        Assert.Equal("depthRange", ex.ParamName);
    }
}

public class OrthographicTests
{
    [Fact]
    public void The_box_maps_to_the_unit_box()
    {
        var right = Matrix4x4<double>.Orthographic(8, 6, 1, 21);
        var left = Matrix4x4<double>.OrthographicLeftHanded(8, 6, 1, 21, DepthRange.MinusOneToOne);

        Assert.Equal(new Vector3<double>(1, 1, 0), right.TransformPoint((4, 3, -1)));
        Assert.Equal(new Vector3<double>(-1, -1, 1), right.TransformPoint((-4, -3, -21)));
        Assert.Equal(new Vector3<double>(0, 0, 0.5), right.TransformPoint((0, 0, -11)));
        Assert.Equal(new Vector3<double>(1, -1, -1), left.TransformPoint((4, -3, 1)));
        Assert.Equal(new Vector3<double>(0, 0, 1), left.TransformPoint((0, 0, 21)));

        // No perspective: the size doesn't depend on the distance.
        Assert.Equal(right.TransformPoint((1, 0, -2)).X, right.TransformPoint((1, 0, -20)).X);
    }

    [Fact]
    public void Pixels_to_the_screen()
    {
        var ui = Matrix4x4<double>.OrthographicOffCenter(0, 1920, 1080, 0, -1, 1);

        Assert.Equal(new Vector3<double>(-1, 1, 0.5), ui.TransformPoint((0, 0, 0)));        // top left
        Assert.Equal(new Vector3<double>(1, -1, 0.5), ui.TransformPoint((1920, 1080, 0)));  // bottom right
        Assert.Equal(new Vector3<double>(0, 0, 0.5), ui.TransformPoint((960, 540, 0)));
    }

    [Fact]
    public void Reverse_depth()
    {
        var projection = Matrix4x4<double>.OrthographicOffCenterLeftHanded(-1, 1, -1, 1, 2, 10, DepthRange.OneToZero);

        Assert.Equal(1, projection.TransformPoint((0, 0, 2)).Z);
        Assert.Equal(0, projection.TransformPoint((0, 0, 10)).Z);
    }

    [Fact]
    public void Agrees_with_System_Numerics()
    {
        Assert.True(Matrix4x4<float>.FromSystemNumerics(System.Numerics.Matrix4x4.CreateOrthographic(8, 6, 0.5f, 30))
            .IsApproximately(Matrix4x4<float>.Orthographic(8, 6, 0.5f, 30)));
        Assert.True(Matrix4x4<float>.FromSystemNumerics(System.Numerics.Matrix4x4.CreateOrthographicLeftHanded(8, 6, 0.5f, 30))
            .IsApproximately(Matrix4x4<float>.OrthographicLeftHanded(8, 6, 0.5f, 30)));
        Assert.True(Matrix4x4<float>.FromSystemNumerics(System.Numerics.Matrix4x4.CreateOrthographicOffCenter(-2, 6, -1, 3, 0.5f, 30))
            .IsApproximately(Matrix4x4<float>.OrthographicOffCenter(-2, 6, -1, 3, 0.5f, 30)));
        Assert.True(Matrix4x4<float>.FromSystemNumerics(System.Numerics.Matrix4x4.CreateOrthographicOffCenterLeftHanded(-2, 6, -1, 3, 0.5f, 30))
            .IsApproximately(Matrix4x4<float>.OrthographicOffCenterLeftHanded(-2, 6, -1, 3, 0.5f, 30)));
    }

    [Fact]
    public void Invalid_arguments()
    {
        Assert.Equal("right", Assert.Throws<ArgumentOutOfRangeException>(() => Matrix4x4<double>.Orthographic(0, 6, 1, 2)).ParamName);
        Assert.Equal("top", Assert.Throws<ArgumentOutOfRangeException>(() => Matrix4x4<double>.OrthographicOffCenter(0, 1, 2, 2, 1, 2)).ParamName);
        Assert.Equal("far", Assert.Throws<ArgumentOutOfRangeException>(() => Matrix4x4<double>.Orthographic(8, 6, 1, 1)).ParamName);
        Assert.Equal("far", Assert.Throws<ArgumentOutOfRangeException>(() => Matrix4x4<double>.Orthographic(8, 6, 1, double.PositiveInfinity)).ParamName);
    }
}

public class CameraPipelineTests
{
    [Fact]
    public void A_point_from_the_model_to_the_screen_and_back()
    {
        var model = Matrix4x4<double>.Compose((0, 0, -3), Quaternion<double>.RotationY(0.5), (1, 1, 1));
        var view = Matrix4x4<double>.LookAt((0, 2, 5), (0, 0, -3), Vector3<double>.UnitY);
        var projection = Matrix4x4<double>.Perspective(Math.PI / 3, 16.0 / 9, 0.1, 100);
        var modelViewProjection = projection * view * model;

        Vector3<double> vertex = (0.5, 0.5, 0.5);
        Vector3<double> screen = modelViewProjection.TransformPoint(vertex);

        Assert.InRange(screen.X, -1, 1);   // visible
        Assert.InRange(screen.Y, -1, 1);
        Assert.InRange(screen.Z, 0, 1);

        // The inverse takes the point on the screen, with its depth, back to the model.
        Assert.True(modelViewProjection.Inverse().TransformPoint(screen).IsApproximately(vertex, absoluteTolerance: 1e-12));
    }

    [Fact]
    public void Mouse_look_turns_the_view()
    {
        // Yaw a quarter turn to the left: what was on the left is now ahead.
        var orientation = Quaternion<double>.RotationY(Math.PI / 2) * Quaternion<double>.RotationX(0);
        Vector3<double> forward = orientation.Rotate(-Vector3<double>.UnitZ);
        var view = Matrix4x4<double>.LookAt(Vector3<double>.Zero, forward, Vector3<double>.UnitY);

        Assert.True(forward.IsApproximately(-Vector3<double>.UnitX));
        Assert.True(view.TransformPoint((-10, 0, 0)).IsApproximately((0, 0, -10)));
    }
}
