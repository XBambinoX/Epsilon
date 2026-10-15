using Xunit;
using Epsilon.LinearAlgebra;

namespace Epsilon.Tests.LinearAlgebra;

public class QuaternionTests
{
    private static readonly Quaternion<double> I = new(1, 0, 0, 0), J = new(0, 1, 0, 0), K = new(0, 0, 1, 0);

    [Fact]
    public void Entries_and_parts()
    {
        var q = new Quaternion<double>(new Vector3<double>(1, 2, 3), 4);
        var (x, y, z, w) = q;

        Assert.Equal(new Quaternion<double>(1, 2, 3, 4), q);
        Assert.Equal((1.0, 2.0, 3.0, 4.0), (x, y, z, w));
        Assert.Equal(new Vector3<double>(1, 2, 3), q.XYZ);
        Assert.Equal(new Quaternion<double>(0, 0, 0, 1), Quaternion<double>.Identity);
        Assert.Equal(new Quaternion<double>(-1, -2, -3, 4), q.Conjugate());
        Assert.Equal(30, q.NormSquared());
        Assert.Equal(70, q.Dot(new Quaternion<double>(5, 6, 7, 8)));
        Assert.Equal(q with { W = 0 }, new Quaternion<double>(1, 2, 3, 0));
    }

    [Fact]
    public void Hamilton_product()
    {
        Quaternion<double> minusOne = new(0, 0, 0, -1);

        Assert.Equal(K, I * J);
        Assert.Equal(I, J * K);
        Assert.Equal(J, K * I);
        Assert.Equal(-K, J * I);   // not commutative
        Assert.Equal(minusOne, I * I);
        Assert.Equal(minusOne, I * J * K);
        Assert.Equal(new Quaternion<double>(1, 2, 3, 4), Quaternion<double>.Identity * new Quaternion<double>(1, 2, 3, 4));
    }

    [Fact]
    public void Integer_entries_stay_exact()
    {
        Quaternion<int> p = new(1, 2, 3, 4), q = new(5, 6, 7, 8);

        Assert.Equal(new Quaternion<int>(24, 48, 48, -6), p * q);
        Assert.Equal(p.NormSquared() * q.NormSquared(), (p * q).NormSquared());   // the norm is multiplicative
        Assert.Equal(new Quaternion<int>(0, 0, 0, 30), p * p.Conjugate());
    }

    [Fact]
    public void Arithmetic_map_equality_and_text()
    {
        Quaternion<double> q = new(1, 2, 3, 4);

        Assert.Equal(new Quaternion<double>(2, 4, 6, 8), q + q);
        Assert.Equal(new Quaternion<double>(0, 0, 0, 0), q - q);
        Assert.Equal(new Quaternion<double>(2, 4, 6, 8), 2 * q);
        Assert.Equal(new Quaternion<double>(2, 4, 6, 8), q * 2);
        Assert.Equal(new Quaternion<double>(0.5, 1, 1.5, 2), q / 2);
        Assert.Equal(new Quaternion<float>(1, 2, 3, 4), q.Map(float.CreateTruncating));
        Assert.Throws<ArgumentNullException>(() => q.Map<double>(null!));

        Assert.True(q == new Quaternion<double>(1, 2, 3, 4));
        Assert.True(q != -q);
        Assert.Equal(q.GetHashCode(), new Quaternion<double>(1, 2, 3, 4).GetHashCode());
        Assert.False(q.Equals((object)new Vector4<double>(1, 2, 3, 4)));
        Assert.Equal("[0, 0, 0, 1]", Quaternion<double>.Identity.ToString());
        Assert.Equal("[0.5, 0, 0, 1]", new Quaternion<double>(0.5, 0, 0, 1).ToString());
    }
}

public class FloatingPointQuaternionTests
{
    private static Vector3<double> RandomVector(Random random) =>
        (random.NextDouble() * 2 - 1, random.NextDouble() * 2 - 1, random.NextDouble() * 2 - 1);

    [Fact]
    public void Rotations_about_the_axes()
    {
        double quarter = Math.PI / 2;

        Assert.True(Quaternion<double>.RotationX(quarter).Rotate(Vector3<double>.UnitY).IsApproximately(Vector3<double>.UnitZ));
        Assert.True(Quaternion<double>.RotationY(quarter).Rotate(Vector3<double>.UnitZ).IsApproximately(Vector3<double>.UnitX));
        Assert.True(Quaternion<double>.RotationZ(quarter).Rotate(Vector3<double>.UnitX).IsApproximately(Vector3<double>.UnitY));
        Assert.Equal(Quaternion<double>.Identity, Quaternion<double>.RotationX(0));
        Assert.Equal(new Quaternion<double>(0, 0, Math.Sin(0.5), Math.Cos(0.5)), Quaternion<double>.RotationZ(1));

        // A third of a turn about the diagonal cycles the axes.
        var cycle = Quaternion<double>.Rotation((1, 1, 1), 2 * Math.PI / 3);
        Assert.True(cycle.Rotate(Vector3<double>.UnitX).IsApproximately(Vector3<double>.UnitY));
        Assert.True(cycle.Rotate(Vector3<double>.UnitY).IsApproximately(Vector3<double>.UnitZ));

        var ex = Assert.Throws<ArgumentException>(() => Quaternion<double>.Rotation(Vector3<double>.Zero, 1));
        Assert.Equal("axis", ex.ParamName);
    }

    [Fact]
    public void Quaternions_and_matrices_agree()
    {
        var random = new Random(6);
        for (int k = 0; k < 50; k++)
        {
            Vector3<double> axis = RandomVector(random), v = RandomVector(random);
            double angle = random.NextDouble() * 10 - 5;
            var q = Quaternion<double>.Rotation(axis, angle);
            var p = Quaternion<double>.Rotation(RandomVector(random), random.NextDouble() * 10 - 5);

            Assert.True(Matrix3x3<double>.Rotation(q).IsApproximately(Matrix3x3<double>.Rotation(axis, angle)));
            Assert.True(q.Rotate(v).IsApproximately(Matrix3x3<double>.Rotation(axis, angle) * v));
            Assert.True(Matrix3x3<double>.Rotation(p * q).IsApproximately(Matrix3x3<double>.Rotation(p) * Matrix3x3<double>.Rotation(q)));
            Assert.True((p * q).Rotate(v).IsApproximately(p.Rotate(q.Rotate(v))));   // q first
        }

        var r = Quaternion<double>.Rotation((1, 2, 3), 0.7);
        Assert.Equal(new Matrix4x4<double>(Matrix3x3<double>.Rotation(r)), Matrix4x4<double>.Rotation(r));
        Assert.Equal(Matrix3x3<double>.Identity, Matrix3x3<double>.Rotation(Quaternion<double>.Identity));
    }

    [Fact]
    public void Any_nonzero_multiple_is_the_same_rotation()
    {
        var q = Quaternion<double>.Rotation((1, -2, 0.5), 2.2);
        Vector3<double> v = (3, 1, -2);

        Assert.True((3 * q).Rotate(v).IsApproximately(q.Rotate(v)));
        Assert.True((-q).Rotate(v).IsApproximately(q.Rotate(v)));
        Assert.True(Matrix3x3<double>.Rotation(0.1 * q).IsApproximately(Matrix3x3<double>.Rotation(q)));

        // Squared norms of 1e400 and 1e-400 would overflow and underflow.
        Assert.True((1e200 * q).Rotate(v).IsApproximately(q.Rotate(v)));
        Assert.True((1e-200 * q).Rotate(v).IsApproximately(q.Rotate(v)));
        Assert.True(Matrix3x3<double>.Rotation(1e200 * q).IsApproximately(Matrix3x3<double>.Rotation(q)));
    }

    [Fact]
    public void The_zero_quaternion_is_no_rotation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => default(Quaternion<double>).Rotate(Vector3<double>.UnitX));
        Assert.Equal("The zero quaternion is no rotation.", ex.Message);

        var argEx = Assert.Throws<ArgumentException>(() => Matrix4x4<double>.Rotation(default(Quaternion<double>)));
        Assert.Equal("rotation", argEx.ParamName);

        Assert.Equal("Cannot normalize the zero quaternion.",
            Assert.Throws<InvalidOperationException>(() => default(Quaternion<double>).Normalize()).Message);
        Assert.Equal("The zero quaternion has no inverse.",
            Assert.Throws<InvalidOperationException>(() => default(Quaternion<double>).Inverse()).Message);
    }

    [Fact]
    public void From_a_rotation_matrix_and_back()
    {
        var random = new Random(7);
        var rotations = new List<Quaternion<double>>();
        for (int k = 0; k < 40; k++)
            rotations.Add(Quaternion<double>.Rotation(RandomVector(random), random.NextDouble() * 2 * Math.PI));

        // Half turns, where 1 + trace is about 0: each branch of Shepperd's method.
        rotations.Add(Quaternion<double>.RotationX(3.1));
        rotations.Add(Quaternion<double>.RotationY(3.1));
        rotations.Add(Quaternion<double>.RotationZ(3.1));
        rotations.Add(Quaternion<double>.Rotation((1, 1, 0), Math.PI));

        foreach (var q in rotations)
        {
            var back = Quaternion<double>.FromRotationMatrix(Matrix3x3<double>.Rotation(q));
            Assert.Equal(1, back.Norm(), 1e-15);
            Assert.True(back.Angle(q) < 1e-7, $"{q} came back as {back}");
            Assert.True(Matrix3x3<double>.Rotation(back).IsApproximately(Matrix3x3<double>.Rotation(q)));
        }

        Assert.Equal(Quaternion<double>.Identity, Quaternion<double>.FromRotationMatrix(Matrix3x3<double>.Identity));
        var turn = Quaternion<double>.Rotation((1, 2, 3), 1);
        Assert.True(Quaternion<double>.FromRotationMatrix(Matrix4x4<double>.Translation((5, 0, 0)) * Matrix4x4<double>.Rotation(turn)).Angle(turn) < 1e-7);
    }

    [Fact]
    public void Norm_normalize_and_inverse()
    {
        var q = Quaternion<double>.Rotation((0, 1, 0), 1) * 3;

        Assert.Equal(3, q.Norm(), 1e-15);
        Assert.Equal(1, q.Normalize().Norm(), 1e-15);
        Assert.Equal(5e300, new Quaternion<double>(3e300, 4e300, 0, 0).Norm());
        Assert.Equal(new Quaternion<double>(0, 0, 0, 0.5), new Quaternion<double>(0, 0, 0, 2).Inverse());
        Assert.True((q * q.Inverse()).IsApproximately(Quaternion<double>.Identity));
        Assert.True((q.Inverse() * q).IsApproximately(Quaternion<double>.Identity));
        Assert.True((new Quaternion<double>(1, 2, 3, 4) * 1e200).Inverse().IsApproximately(new Quaternion<double>(1, 2, 3, 4).Inverse() / 1e200));
        Assert.Equal(q.Normalize().Conjugate(), q.Normalize().Inverse());
    }

    [Fact]
    public void Axis_and_angle()
    {
        var (axis, angle) = Quaternion<double>.Rotation((0, 0, 2), 1).ToAxisAngle();
        Assert.True(axis.IsApproximately(Vector3<double>.UnitZ));
        Assert.Equal(1, angle, 1e-15);

        // More than half a turn comes back as the shorter turn the other way.
        (axis, angle) = Quaternion<double>.RotationZ(4).ToAxisAngle();
        Assert.True(axis.IsApproximately(-Vector3<double>.UnitZ));
        Assert.Equal(2 * Math.PI - 4, angle, 1e-14);

        Assert.Equal((Vector3<double>.UnitX, 0.0), Quaternion<double>.Identity.ToAxisAngle());
        Assert.Equal(Math.PI, Quaternion<double>.RotationX(Math.PI).ToAxisAngle().Angle, 1e-15);
    }

    [Fact]
    public void Angle_between_rotations()
    {
        var q = Quaternion<double>.Rotation((1, 2, 3), 0.9);

        Assert.Equal(0.7, Quaternion<double>.RotationZ(0.3).Angle(Quaternion<double>.RotationZ(1)), 1e-15);
        Assert.Equal(0, q.Angle(-q));
        Assert.Equal(0, q.Angle(5 * q), 1e-15);
        Assert.Equal(Math.PI, Quaternion<double>.Identity.Angle(Quaternion<double>.RotationY(Math.PI)), 1e-15);

        // The arccosine of the dot product gives 0 here: cos(2.5e-11) rounds to 1.
        Assert.Equal(1e-10, Quaternion<double>.RotationZ(1e-10).Angle(Quaternion<double>.Identity), 1e-24);
    }

    [Fact]
    public void Slerp_turns_at_constant_speed()
    {
        var start = Quaternion<double>.RotationZ(0.2);
        var end = Quaternion<double>.Rotation((1, 1, 0), 2.6);
        double total = start.Angle(end);

        Assert.Equal(start, Quaternion<double>.Slerp(start, end, 0));
        Assert.True(Quaternion<double>.Slerp(start, end, 1).IsApproximately(end));
        foreach (double t in new[] { 0.1, 0.25, 0.5, 0.9 })
        {
            var between = Quaternion<double>.Slerp(start, end, t);
            Assert.Equal(1, between.Norm(), 1e-15);
            Assert.Equal(t * total, start.Angle(between), 1e-12);
            Assert.Equal((1 - t) * total, between.Angle(end), 1e-12);
        }

        Assert.True(Quaternion<double>.Slerp(Quaternion<double>.Identity, Quaternion<double>.RotationZ(1), 0.5).IsApproximately(Quaternion<double>.RotationZ(0.5)));
        Assert.Equal(start, Quaternion<double>.Slerp(start, start, 0.3));
        Assert.True(Quaternion<double>.Slerp(Quaternion<double>.Identity, Quaternion<double>.RotationZ(1e-12), 0.5).IsApproximately(Quaternion<double>.RotationZ(5e-13)));
    }

    [Fact]
    public void Interpolation_goes_the_shorter_way()
    {
        // -q is the same rotation as q, so the way to it is short.
        var end = -Quaternion<double>.RotationZ(1);

        Assert.True(Quaternion<double>.Slerp(Quaternion<double>.Identity, end, 0.5).Angle(Quaternion<double>.RotationZ(0.5)) < 1e-12);
        Assert.True(Quaternion<double>.Nlerp(Quaternion<double>.Identity, end, 0.5).Angle(Quaternion<double>.RotationZ(0.5)) < 1e-12);
        Assert.True(Quaternion<double>.Slerp(Quaternion<double>.Identity, end, 1).IsApproximately(Quaternion<double>.RotationZ(1)));
    }

    [Fact]
    public void Nlerp_follows_the_same_path()
    {
        var start = Quaternion<double>.RotationX(0.4);
        var end = Quaternion<double>.Rotation((0, 1, 1), 1.5);
        var nlerp = Quaternion<double>.Nlerp(start, end, 0.3);
        var slerp = Quaternion<double>.Slerp(start, end, 0.3);

        Assert.Equal(1, nlerp.Norm(), 1e-15);
        Assert.True(Quaternion<double>.Nlerp(start, end, 0.5).IsApproximately(Quaternion<double>.Slerp(start, end, 0.5)));
        Assert.True(nlerp.Angle(slerp) < 0.02);   // close, but not at constant speed
        Assert.True(nlerp.Angle(slerp) > 1e-6);
    }

    [Fact]
    public void Approximate_equality()
    {
        var q = Quaternion<double>.RotationZ(Math.PI / 2);
        var exact = new Quaternion<double>(0, 0, Math.Sqrt(0.5), Math.Sqrt(0.5));

        Assert.True(q.IsApproximately(exact));
        Assert.False(q.IsApproximately(-q));   // the same rotation, but not close as quaternions
        Assert.True(q.IsApproximately(q * (1 + 1e-6), relativeTolerance: 1e-3));
        Assert.True(Quaternion<float>.RotationZ(MathF.PI / 2).IsApproximately(exact.Map(float.CreateTruncating)));
        Assert.Throws<ArgumentOutOfRangeException>(() => q.IsApproximately(q, -1));
    }

    [Fact]
    public void Float_and_Half_entries()
    {
        var q = Quaternion<float>.Rotation((1, 2, 3), 1.1f);
        Assert.True((q * q.Inverse()).IsApproximately(Quaternion<float>.Identity, absoluteTolerance: 1e-6f));
        Assert.True(Quaternion<float>.FromRotationMatrix(Matrix3x3<float>.Rotation(q)).Angle(q) < 1e-3f);

        var h = Quaternion<Half>.RotationZ((Half)1);
        Assert.True(h.Rotate(Vector3<Half>.UnitX).IsApproximately(((Half)Math.Cos(1), (Half)Math.Sin(1), (Half)0)));
    }
}

public class QuaternionSystemNumericsTests
{
    [Fact]
    public void Conversions_keep_the_entries()
    {
        var q = new Quaternion<float>(1, 2, 3, 4);

        Assert.Equal(new System.Numerics.Quaternion(1, 2, 3, 4), q.ToSystemNumerics());
        Assert.Equal(q, Quaternion<float>.FromSystemNumerics(new System.Numerics.Quaternion(1, 2, 3, 4)));
        Assert.Equal(System.Numerics.Quaternion.Identity, Quaternion<float>.Identity.ToSystemNumerics());
    }

    [Fact]
    public void System_Numerics_rotates_the_same_way()
    {
        var axis = new System.Numerics.Vector3(1, 2, 3);
        var numerics = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.Normalize(axis), 0.7f);
        var ours = Quaternion<float>.Rotation((1, 2, 3), 0.7f);
        var point = new System.Numerics.Vector3(4, -1, 2);

        Assert.True(Quaternion<float>.FromSystemNumerics(numerics).IsApproximately(ours));
        Assert.True(Vector3<float>.FromSystemNumerics(System.Numerics.Vector3.Transform(point, numerics)).IsApproximately(ours.Rotate((4, -1, 2))));
        Assert.True(Matrix4x4<float>.FromSystemNumerics(System.Numerics.Matrix4x4.CreateFromQuaternion(numerics)).IsApproximately(Matrix4x4<float>.Rotation(ours)));
        Assert.True(Quaternion<float>.FromSystemNumerics(System.Numerics.Quaternion.CreateFromRotationMatrix(Matrix4x4<float>.Rotation(ours).ToSystemNumerics())).Angle(ours) < 1e-3f);
    }

    [Fact]
    public void System_Numerics_products_and_slerp_agree()
    {
        var p = System.Numerics.Quaternion.CreateFromYawPitchRoll(0.3f, 1.1f, -0.4f);
        var q = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitZ, 2.1f);
        Quaternion<float> ourP = Quaternion<float>.FromSystemNumerics(p), ourQ = Quaternion<float>.FromSystemNumerics(q);

        Assert.True(Quaternion<float>.FromSystemNumerics(p * q).IsApproximately(ourP * ourQ));
        Assert.True(Quaternion<float>.FromSystemNumerics(System.Numerics.Quaternion.Slerp(p, q, 0.3f)).IsApproximately(Quaternion<float>.Slerp(ourP, ourQ, 0.3f)));
        Assert.True(Quaternion<float>.FromSystemNumerics(System.Numerics.Quaternion.Inverse(p)).IsApproximately(ourP.Inverse()));
    }
}

public class EulerAngleTests
{
    private static readonly EulerOrder[] Orders = Enum.GetValues<EulerOrder>();

    // The definition, written out for every order: about the first axis of the name first.
    private static Matrix3x3<double> Composed(Vector3<double> angles, EulerOrder order)
    {
        var x = Matrix3x3<double>.RotationX(angles.X);
        var y = Matrix3x3<double>.RotationY(angles.Y);
        var z = Matrix3x3<double>.RotationZ(angles.Z);
        return order switch
        {
            EulerOrder.XYZ => z * y * x,
            EulerOrder.XZY => y * z * x,
            EulerOrder.YXZ => z * x * y,
            EulerOrder.YZX => x * z * y,
            EulerOrder.ZXY => y * x * z,
            EulerOrder.ZYX => x * y * z,
            _ => throw new ArgumentOutOfRangeException(nameof(order)),
        };
    }

    // The index of the middle axis of the order: the angle in [-pi/2, pi/2].
    private static int Middle(EulerOrder order) => "XYZ".IndexOf(order.ToString()[1]);

    private static Vector3<double> RandomAngles(Random random, EulerOrder order)
    {
        double[] angles = [random.NextDouble() * 6 - 3, random.NextDouble() * 6 - 3, random.NextDouble() * 6 - 3];
        angles[Middle(order)] = random.NextDouble() * 3 - 1.5;
        return (angles[0], angles[1], angles[2]);
    }

    [Fact]
    public void Euler_angles_turn_about_the_fixed_axes_in_the_order_of_the_name()
    {
        Vector3<double> angles = (0.3, -1.1, 2.2);
        foreach (var order in Orders)
        {
            Assert.Equal(Composed(angles, order), Matrix3x3<double>.FromEulerAngles(angles, order));
            Assert.Equal(new Matrix4x4<double>(Composed(angles, order)), Matrix4x4<double>.FromEulerAngles(angles, order));
            Assert.True(Matrix3x3<double>.Rotation(Quaternion<double>.FromEulerAngles(angles, order)).IsApproximately(Composed(angles, order)));
        }

        Assert.Equal(Quaternion<double>.RotationZ(2.2) * Quaternion<double>.RotationY(-1.1) * Quaternion<double>.RotationX(0.3),
            Quaternion<double>.FromEulerAngles(angles, EulerOrder.XYZ));
    }

    [Fact]
    public void Euler_angles_come_back()
    {
        var random = new Random(8);
        foreach (var order in Orders)
        {
            for (int k = 0; k < 30; k++)
            {
                Vector3<double> angles = RandomAngles(random, order);

                Assert.True(Matrix3x3<double>.FromEulerAngles(angles, order).ToEulerAngles(order).IsApproximately(angles, absoluteTolerance: 1e-12), $"{order} {angles}");
                Assert.True(Quaternion<double>.FromEulerAngles(angles, order).ToEulerAngles(order).IsApproximately(angles, absoluteTolerance: 1e-12), $"{order} {angles}");
                Assert.True(Matrix4x4<double>.FromEulerAngles(angles, order).ToEulerAngles(order).IsApproximately(angles, absoluteTolerance: 1e-12));
            }
        }
    }

    [Fact]
    public void Gimbal_lock_keeps_the_rotation()
    {
        foreach (var order in Orders)
        {
            foreach (double middle in new[] { Math.PI / 2, -Math.PI / 2, Math.PI / 2 - 1e-10, -Math.PI / 2 + 1e-9 })
            {
                double[] values = [0.4, -0.9, 1.3];
                values[Middle(order)] = middle;
                Vector3<double> angles = (values[0], values[1], values[2]);
                var rotation = Matrix3x3<double>.FromEulerAngles(angles, order);

                Vector3<double> back = rotation.ToEulerAngles(order);
                Assert.True(Matrix3x3<double>.FromEulerAngles(back, order).IsApproximately(rotation, absoluteTolerance: 1e-7), $"{order} {middle}: {back}");
                Assert.Equal(middle, back[Middle(order)], 1e-7);
            }
        }

        // Exactly at the lock the last angle of the order is 0.
        Vector3<double> locked = Matrix3x3<double>.FromEulerAngles((0.4, Math.PI / 2, 1.3), EulerOrder.XYZ).ToEulerAngles(EulerOrder.XYZ);
        Assert.Equal(0, locked.Z);
        Assert.Equal(0.4 - 1.3, locked.X, 1e-12);
    }

    [Fact]
    public void Yaw_pitch_and_roll_of_System_Numerics()
    {
        float yaw = 0.7f, pitch = -0.3f, roll = 1.9f;

        Assert.True(Quaternion<float>.FromSystemNumerics(System.Numerics.Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll))
            .IsApproximately(Quaternion<float>.FromEulerAngles((pitch, yaw, roll), EulerOrder.ZXY)));
        Assert.True(Matrix4x4<float>.FromSystemNumerics(System.Numerics.Matrix4x4.CreateFromYawPitchRoll(yaw, pitch, roll))
            .IsApproximately(Matrix4x4<float>.FromEulerAngles((pitch, yaw, roll), EulerOrder.ZXY)));
    }

    [Fact]
    public void Invalid_input()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Matrix3x3<double>.FromEulerAngles(Vector3<double>.Zero, (EulerOrder)42));
        Assert.Equal("order", ex.ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => Quaternion<double>.Identity.ToEulerAngles((EulerOrder)(-1)));
        Assert.Throws<InvalidOperationException>(() => default(Quaternion<double>).ToEulerAngles(EulerOrder.XYZ));

        // The translation of a 4 x 4 matrix doesn't matter.
        var moved = Matrix4x4<double>.Translation((5, 6, 7)) * Matrix4x4<double>.FromEulerAngles((0.1, 0.2, 0.3), EulerOrder.YXZ);
        Assert.True(moved.ToEulerAngles(EulerOrder.YXZ).IsApproximately((0.1, 0.2, 0.3)));
    }
}

public class FromToRotationTests
{
    [Fact]
    public void Turns_one_direction_into_the_other_the_shortest_way()
    {
        var random = new Random(9);
        for (int k = 0; k < 50; k++)
        {
            Vector3<double> from = (random.NextDouble() - 0.5, random.NextDouble() - 0.5, random.NextDouble() - 0.5);
            Vector3<double> to = 3 * new Vector3<double>(random.NextDouble() - 0.5, random.NextDouble() - 0.5, random.NextDouble() - 0.5);
            var q = Quaternion<double>.FromToRotation(from, to);

            Assert.Equal(1, q.Norm(), 1e-15);
            Assert.True(q.Rotate(from.Normalize()).IsApproximately(to.Normalize()));
            Assert.Equal(from.Angle(to), q.ToAxisAngle().Angle, 1e-12);
        }

        Assert.True(Quaternion<double>.FromToRotation(Vector3<double>.UnitX, Vector3<double>.UnitY).IsApproximately(Quaternion<double>.RotationZ(Math.PI / 2)));
        Assert.True(Quaternion<double>.FromToRotation((1, 2, 3), (2, 4, 6)).IsApproximately(Quaternion<double>.Identity));
    }

    [Fact]
    public void Opposite_directions_take_half_a_turn()
    {
        foreach (Vector3<double> v in new Vector3<double>[] { Vector3<double>.UnitX, (0, -2, 0), (1, 2, 3) })
        {
            var q = Quaternion<double>.FromToRotation(v, -v);
            Assert.True(q.Rotate(v).IsApproximately(-v));
            Assert.Equal(Math.PI, q.ToAxisAngle().Angle, 1e-15);
            Assert.Equal(0, q.ToAxisAngle().Axis.Dot(v), 1e-15);
        }
    }

    [Fact]
    public void Nearly_opposite_directions_stay_accurate()
    {
        // 1 + a . b rounds to 0 here, which would turn UnitX to exactly -UnitX.
        Vector3<double> to = (-1, 1e-9, 0);
        Assert.Equal(0, 1 + Vector3<double>.UnitX.Dot(to.Normalize()));

        Vector3<double> turned = Quaternion<double>.FromToRotation(Vector3<double>.UnitX, to).Rotate(Vector3<double>.UnitX);
        Assert.Equal(-1, turned.X, 1e-15);
        Assert.Equal(1e-9, turned.Y, 1e-22);
    }

    [Fact]
    public void Zero_vectors_have_no_direction()
    {
        Assert.Equal("from", Assert.Throws<ArgumentException>(() => Quaternion<double>.FromToRotation(Vector3<double>.Zero, Vector3<double>.UnitX)).ParamName);
        Assert.Equal("to", Assert.Throws<ArgumentException>(() => Quaternion<double>.FromToRotation(Vector3<double>.UnitX, Vector3<double>.Zero)).ParamName);
    }
}
