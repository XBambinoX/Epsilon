namespace Epsilon.LinearAlgebra;

// Euler angles of rotation matrices, shared by the matrices and the quaternions.
internal static class EulerAngles
{
    // The axes (0 for x, 1 for y, 2 for z) in the order of the rotations, and 1 or -1 as they are
    // an even or an odd permutation of x, y, z.
    internal static (int First, int Second, int Third, int Parity) Axes(EulerOrder order) => order switch
    {
        EulerOrder.XYZ => (0, 1, 2, 1),
        EulerOrder.YZX => (1, 2, 0, 1),
        EulerOrder.ZXY => (2, 0, 1, 1),
        EulerOrder.XZY => (0, 2, 1, -1),
        EulerOrder.YXZ => (1, 0, 2, -1),
        EulerOrder.ZYX => (2, 1, 0, -1),
        _ => throw new ArgumentOutOfRangeException(nameof(order), order, "Not an Euler order."),
    };

    // The angles (about x, y and z) of the rotation matrix R = Rc Rb Ra, for the axes a, b, c of
    // the order. The middle angle is in [-pi/2, pi/2], the others in [-pi, pi]. Where its cosine is
    // tiny (gimbal lock) only the sum or the difference of the other two counts, and the third one
    // is 0: the regular formulas would divide rounding noise by that cosine.
    internal static Vector3<T> Of<T>(Matrix3x3<T> m, EulerOrder order) where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        var (a, b, c, parity) = Axes(order);
        T s = parity > 0 ? T.One : -T.One;

        T cosine = T.Hypot(m[c, b], m[c, c]);
        T middle = T.Atan2(-s * m[c, a], cosine);
        T first, third;
        if (cosine > FloatingPoint<T>.SqrtMachineEpsilon)
        {
            first = T.Atan2(s * m[c, b], m[c, c]);
            third = T.Atan2(s * m[b, a], m[a, a]);
        }
        else
        {
            first = T.Atan2(-s * m[b, c], m[b, b]);
            third = T.Zero;
        }

        var angles = new T[3];
        angles[a] = first;
        angles[b] = middle;
        angles[c] = third;
        return new(angles[0], angles[1], angles[2]);
    }
}
