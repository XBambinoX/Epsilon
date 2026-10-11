namespace Epsilon.LinearAlgebra;

/// <summary>
/// The order in which Euler angles turn about the axes of space. The rotations are about the
/// fixed axes, first about the first axis of the name: <see cref="XYZ"/> turns about x, then
/// about y, then about z, so its matrix is Rz Ry Rx. The same rotation is the reverse order about
/// the axes that turn with the object: about z, then about the new y, then about the newest x.
/// </summary>
public enum EulerOrder
{
    /// <summary>About x, then y, then z: the matrix Rz Ry Rx.</summary>
    XYZ,

    /// <summary>About x, then z, then y: the matrix Ry Rz Rx.</summary>
    XZY,

    /// <summary>About y, then x, then z: the matrix Rz Rx Ry.</summary>
    YXZ,

    /// <summary>About y, then z, then x: the matrix Rx Rz Ry.</summary>
    YZX,

    /// <summary>
    /// About z, then x, then y: the matrix Ry Rx Rz. With the angles (pitch, yaw, roll) it is
    /// <c>CreateFromYawPitchRoll</c> of System.Numerics.
    /// </summary>
    ZXY,

    /// <summary>About z, then y, then x: the matrix Rx Ry Rz.</summary>
    ZYX,
}
