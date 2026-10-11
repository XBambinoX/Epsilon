namespace Epsilon.LinearAlgebra;

/// <summary>
/// View and projection matrices of a camera, of float, double or Half. The plain ones are
/// right-handed: the camera looks along -z, with x to the right and y up, as in OpenGL and glTF.
/// The <c>LeftHanded</c> ones look along +z, as in Direct3D. The projections map what the camera
/// sees to x and y in [-1, 1] and to the depths of a <see cref="DepthRange"/>, after
/// <see cref="Matrix4x4{T}.TransformPoint"/> divides by w.
/// </summary>
public static class CameraMatrixExtensions
{
    extension<T>(Matrix4x4<T> matrix) where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        /// <summary>
        /// The view matrix of a camera at <paramref name="eye"/> that looks at
        /// <paramref name="target"/>, with <paramref name="up"/> pointing up on the screen: it moves
        /// the world so that the camera is at the origin, looks along -z and has y up. It is the
        /// inverse of the camera's own transform.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="target"/> is <paramref name="eye"/>, or <paramref name="up"/> is parallel to the direction of view.
        /// </exception>
        public static Matrix4x4<T> LookAt(Vector3<T> eye, Vector3<T> target, Vector3<T> up)
        {
            var (right, trueUp, forward) = Basis(eye, target, up, rightHanded: true);
            return View(right, trueUp, -forward, eye);
        }

        /// <summary>
        /// The view matrix of a camera at <paramref name="eye"/> that looks at
        /// <paramref name="target"/>, left-handed: the camera ends up at the origin, looking along +z
        /// with y up.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="target"/> is <paramref name="eye"/>, or <paramref name="up"/> is parallel to the direction of view.
        /// </exception>
        public static Matrix4x4<T> LookAtLeftHanded(Vector3<T> eye, Vector3<T> target, Vector3<T> up)
        {
            var (right, trueUp, forward) = Basis(eye, target, up, rightHanded: false);
            return View(right, trueUp, forward, eye);
        }

        /// <summary>
        /// The perspective projection of a camera that looks along -z: <paramref name="fieldOfViewY"/>
        /// is the vertical angle of view in radians, <paramref name="aspectRatio"/> the width over the
        /// height. Points at the distance <paramref name="near"/> get the near depth, those at
        /// <paramref name="far"/> the far one; <paramref name="far"/> may be infinite.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The angle is not between 0 and pi, the aspect ratio not positive, not 0 &lt; near &lt; far,
        /// or <paramref name="depthRange"/> is not a depth range.
        /// </exception>
        public static Matrix4x4<T> Perspective(T fieldOfViewY, T aspectRatio, T near, T far, DepthRange depthRange = DepthRange.ZeroToOne) =>
            PerspectiveMatrix(fieldOfViewY, aspectRatio, near, far, depthRange, rightHanded: true);

        /// <summary>
        /// The perspective projection of a camera that looks along +z; see <see cref="Perspective"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The angle is not between 0 and pi, the aspect ratio not positive, not 0 &lt; near &lt; far,
        /// or <paramref name="depthRange"/> is not a depth range.
        /// </exception>
        public static Matrix4x4<T> PerspectiveLeftHanded(T fieldOfViewY, T aspectRatio, T near, T far, DepthRange depthRange = DepthRange.ZeroToOne) =>
            PerspectiveMatrix(fieldOfViewY, aspectRatio, near, far, depthRange, rightHanded: false);

        /// <summary>
        /// The orthographic projection of a camera that looks along -z: a box of
        /// <paramref name="width"/> by <paramref name="height"/> around the axis of view, from the
        /// distance <paramref name="near"/> to <paramref name="far"/>. Sizes don't shrink with the
        /// distance.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// A size is 0, near equals far, a value is not finite, or <paramref name="depthRange"/> is not a depth range.
        /// </exception>
        public static Matrix4x4<T> Orthographic(T width, T height, T near, T far, DepthRange depthRange = DepthRange.ZeroToOne) =>
            OrthographicMatrix(-width / FloatingPoint<T>.Two, width / FloatingPoint<T>.Two,
                -height / FloatingPoint<T>.Two, height / FloatingPoint<T>.Two, near, far, depthRange, rightHanded: true);

        /// <summary>
        /// The orthographic projection of a camera that looks along +z; see <see cref="Orthographic"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// A size is 0, near equals far, a value is not finite, or <paramref name="depthRange"/> is not a depth range.
        /// </exception>
        public static Matrix4x4<T> OrthographicLeftHanded(T width, T height, T near, T far, DepthRange depthRange = DepthRange.ZeroToOne) =>
            OrthographicMatrix(-width / FloatingPoint<T>.Two, width / FloatingPoint<T>.Two,
                -height / FloatingPoint<T>.Two, height / FloatingPoint<T>.Two, near, far, depthRange, rightHanded: false);

        /// <summary>
        /// The orthographic projection of the box from <paramref name="left"/> to
        /// <paramref name="right"/>, <paramref name="bottom"/> to <paramref name="top"/> and the
        /// distance <paramref name="near"/> to <paramref name="far"/>, looking along -z.
        /// <c>OrthographicOffCenter(0, 1920, 1080, 0, -1, 1)</c> maps pixels to the screen, with
        /// (0, 0) at the top left.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Left equals right, bottom equals top, near equals far, a value is not finite, or
        /// <paramref name="depthRange"/> is not a depth range.
        /// </exception>
        public static Matrix4x4<T> OrthographicOffCenter(T left, T right, T bottom, T top, T near, T far, DepthRange depthRange = DepthRange.ZeroToOne) =>
            OrthographicMatrix(left, right, bottom, top, near, far, depthRange, rightHanded: true);

        /// <summary>
        /// The orthographic projection of a box, looking along +z; see <see cref="OrthographicOffCenter"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Left equals right, bottom equals top, near equals far, a value is not finite, or
        /// <paramref name="depthRange"/> is not a depth range.
        /// </exception>
        public static Matrix4x4<T> OrthographicOffCenterLeftHanded(T left, T right, T bottom, T top, T near, T far, DepthRange depthRange = DepthRange.ZeroToOne) =>
            OrthographicMatrix(left, right, bottom, top, near, far, depthRange, rightHanded: false);
    }

    // The unit axes of a camera: right, up and the direction of view.
    private static (Vector3<T> Right, Vector3<T> Up, Vector3<T> Forward) Basis<T>(Vector3<T> eye, Vector3<T> target, Vector3<T> up, bool rightHanded)
        where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        Vector3<T> view = target - eye;
        if (view == Vector3<T>.Zero)
            throw new ArgumentException("The eye and the target are the same point.", nameof(target));

        Vector3<T> forward = view.Normalize();
        Vector3<T> side = rightHanded ? forward.Cross(up) : up.Cross(forward);
        if (side == Vector3<T>.Zero)
            throw new ArgumentException("The up direction is parallel to the direction of view.", nameof(up));

        Vector3<T> right = side.Normalize();
        return (right, rightHanded ? right.Cross(forward) : forward.Cross(right), forward);
    }

    // The matrix whose rows are the axes of the camera, which then move the eye to the origin.
    private static Matrix4x4<T> View<T>(Vector3<T> x, Vector3<T> y, Vector3<T> z, Vector3<T> eye)
        where T : struct, System.Numerics.IFloatingPointIeee754<T> => new(
        x.X, x.Y, x.Z, -x.Dot(eye),
        y.X, y.Y, y.Z, -y.Dot(eye),
        z.X, z.Y, z.Z, -z.Dot(eye),
        T.Zero, T.Zero, T.Zero, T.One);

    // The depths of the near and the far plane.
    private static (T Near, T Far) Depths<T>(DepthRange depthRange) where T : struct, System.Numerics.IFloatingPointIeee754<T> =>
        depthRange switch
        {
            DepthRange.ZeroToOne => (T.Zero, T.One),
            DepthRange.MinusOneToOne => (-T.One, T.One),
            DepthRange.OneToZero => (T.One, T.Zero),
            _ => throw new ArgumentOutOfRangeException(nameof(depthRange), depthRange, "Not a depth range."),
        };

    // x and y scaled by the cotangent of half the angle; w is the distance along the axis of view.
    // The depth row (A, B) gives (A z + B) / w the near depth dn at the distance n and the far one df
    // at f: right-handed A = dn q - df r and B = (dn - df) n r with q = n / (f - n) and r = f / (f - n),
    // which tend to 0 and 1 for an infinite f; left-handed A is negated.
    private static Matrix4x4<T> PerspectiveMatrix<T>(T fieldOfViewY, T aspectRatio, T near, T far, DepthRange depthRange, bool rightHanded)
        where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        if (!(fieldOfViewY > T.Zero && fieldOfViewY < T.Pi))
            throw new ArgumentOutOfRangeException(nameof(fieldOfViewY), fieldOfViewY, "The angle of view must be between 0 and pi.");
        if (!(aspectRatio > T.Zero) || !T.IsFinite(aspectRatio))
            throw new ArgumentOutOfRangeException(nameof(aspectRatio), aspectRatio, "The aspect ratio must be a positive number.");
        if (!(near > T.Zero) || !T.IsFinite(near))
            throw new ArgumentOutOfRangeException(nameof(near), near, "The near distance must be a positive number.");
        if (!(far > near))
            throw new ArgumentOutOfRangeException(nameof(far), far, "The far distance must be larger than the near one.");

        var (dn, df) = Depths<T>(depthRange);
        T yScale = T.One / T.Tan(fieldOfViewY / FloatingPoint<T>.Two);
        T xScale = yScale / aspectRatio;
        T q = T.IsPositiveInfinity(far) ? T.Zero : near / (far - near);
        T r = T.IsPositiveInfinity(far) ? T.One : far / (far - near);
        T a = dn * q - df * r;
        T b = (dn - df) * near * r;
        T w = rightHanded ? -T.One : T.One;
        return new(
            xScale, T.Zero, T.Zero, T.Zero,
            T.Zero, yScale, T.Zero, T.Zero,
            T.Zero, T.Zero, rightHanded ? a : -a, b,
            T.Zero, T.Zero, w, T.Zero);
    }

    // The box maps to [-1, 1] in x and y; the depth is linear in z, with the near depth at the
    // distance near, which is z = -near right-handed and z = near left-handed.
    private static Matrix4x4<T> OrthographicMatrix<T>(T left, T right, T bottom, T top, T near, T far, DepthRange depthRange, bool rightHanded)
        where T : struct, System.Numerics.IFloatingPointIeee754<T>
    {
        if (!T.IsFinite(left) || !T.IsFinite(right) || left == right)
            throw new ArgumentOutOfRangeException(nameof(right), right, "Left and right must be different numbers.");
        if (!T.IsFinite(bottom) || !T.IsFinite(top) || bottom == top)
            throw new ArgumentOutOfRangeException(nameof(top), top, "Bottom and top must be different numbers.");
        if (!T.IsFinite(near) || !T.IsFinite(far) || near == far)
            throw new ArgumentOutOfRangeException(nameof(far), far, "Near and far must be different numbers.");

        var (dn, df) = Depths<T>(depthRange);
        T two = FloatingPoint<T>.Two;
        T scale = (df - dn) / (far - near);   // per unit of distance
        T c = rightHanded ? -scale : scale;
        return new(
            two / (right - left), T.Zero, T.Zero, -(right + left) / (right - left),
            T.Zero, two / (top - bottom), T.Zero, -(top + bottom) / (top - bottom),
            T.Zero, T.Zero, c, dn - scale * near,
            T.Zero, T.Zero, T.Zero, T.One);
    }
}
