# Rotations

Rotations of space as quaternions and as matrices, Euler angles, and the model matrix of an
object put together from translation, rotation and scale. The examples use the aliases of
[Fixed-size vectors and matrices](fixed-size.md#entry-types):

```csharp
using Vec3 = Epsilon.LinearAlgebra.Vector3<double>;
using Mat4 = Epsilon.LinearAlgebra.Matrix4x4<double>;
```

## Quaternions

`Quaternion<T>` holds a rotation in four numbers, x i + y j + z k + w: the unit quaternion
(sin(a/2) u, cos(a/2)) turns by the angle a about the unit axis u. Quaternions compose
cheaply, renormalize without the drift of a product of many matrices, and interpolate smoothly:

```csharp
var quarter = Quaternion<double>.RotationY(Math.PI / 2);   // a quarter turn about y
quarter.Rotate((1, 0, 0)).IsApproximately((0, 0, -1));      // true
var half = quarter * quarter;                               // p * q turns by q first, then by p
half.ToAxisAngle();                                         // ([0, 1, 0], 3.1415926535897927)
quarter.Angle(-quarter);                                    // 0: -q is the same rotation
Mat4.Rotation(quarter);                                     // the same rotation as a matrix
Quaternion<double>.FromRotationMatrix(Mat4.Rotation(quarter)).Angle(quarter);   // 0
Quaternion<double>.FromToRotation(Vec3.UnitX, (0, 0, -1)).IsApproximately(quarter);   // true
```

| Member | Result |
|---|---|
| `Rotation(axis, angle)`, `RotationX`, `RotationY`, `RotationZ` | The rotations, by the right-hand rule like those of the matrices. |
| `FromToRotation(from, to)` | The shortest rotation from one direction to another; a half turn for opposite ones. |
| `FromRotationMatrix(m)`, `Mat4.Rotation(q)` | From a rotation matrix and back. |
| `p * q`, `Conjugate()`, `Inverse()` | The product applies q first, as for matrices; the inverse undoes a rotation. |
| `Rotate(v)` | The vector turned: q v q^-1. |
| `ToAxisAngle()`, `Angle(other)` | The axis and the angle, from 0 to pi; the angle between two rotations. |
| `Slerp(start, end, t)`, `Nlerp(start, end, t)` | Interpolation the shorter way round: at constant angular speed, or faster along the same path. |

The inverse undoes a rotation; for a unit quaternion it is the conjugate:

```csharp
quarter * quarter.Inverse();                                   // [0, 0, 0, 1], the identity
quarter.Inverse().Rotate(quarter.Rotate((1, 2, 3))).IsApproximately((1, 2, 3));   // true
```

Wherever a quaternion stands for a rotation it need not have norm 1: q, 3q and -q are the same
rotation. `IsApproximately` compares the four numbers, so q and -q are not close; compare
rotations by `Angle`, which is accurate for small angles too.

## Interpolation

`Slerp` turns from one rotation to another at a constant angular speed, as an animation between
two key frames needs; `Nlerp` follows the same path faster, but its speed varies a little:

```csharp
var identity = Quaternion<double>.Identity;
var turn = Quaternion<double>.RotationZ(Math.PI / 2);
Quaternion<double>.Slerp(identity, turn, 0.5).ToAxisAngle();       // ([0, 0, 1], 0.7853981633974483): half of it
Quaternion<double>.Slerp(identity, turn, 0.25).Angle(identity);    // 0.3926990816987241: a quarter, pi / 8
Quaternion<double>.Nlerp(identity, turn, 0.25).Angle(identity);    // 0.3769590215412104: a little less
```

Both take the shorter way round: from q to -q they don't move at all, as the two are the same
rotation.

## Euler angles

Euler angles turn about the three axes one after the other, and the order matters. It is always
given: `EulerOrder.XYZ` turns about x, then y, then z, all about the fixed axes, so its matrix is
Rz Ry Rx; the same rotation is about z, then the new y, then the newest x of the turning object.
`angles.X` is always the angle about x, whatever the order:

```csharp
Vec3 angles = (0.1, 0.5, 0);                                     // pitch about x, yaw about y
var q = Quaternion<double>.FromEulerAngles(angles, EulerOrder.ZXY);
q.ToEulerAngles(EulerOrder.ZXY).IsApproximately(angles);          // true
Mat4.FromEulerAngles(angles, EulerOrder.ZXY);                     // the same rotation as a matrix

Matrix3x3<double>.FromEulerAngles((0.1, 0.2, 0.3), EulerOrder.XYZ)
    == Matrix3x3<double>.RotationZ(0.3) * Matrix3x3<double>.RotationY(0.2) * Matrix3x3<double>.RotationX(0.1);   // true
```

The middle angle of the order comes back in [-pi/2, pi/2], the others in [-pi, pi]. At gimbal
lock, where the middle angle is pi/2 or -pi/2, only the sum or the difference of the other two
counts, and the last one comes back as 0. `CreateFromYawPitchRoll(yaw, pitch, roll)` of
System.Numerics is `FromEulerAngles((pitch, yaw, roll), EulerOrder.ZXY)`.

## Translation, rotation and scale

`Compose` builds the usual model matrix of an object, T R S: scale, then turn, then move.
`TryDecompose` takes an affine matrix apart again, as for a child in a hierarchy, whose world
matrix is the product of its parent's and its own, or to interpolate between two transforms
part by part:

```csharp
var model = Mat4.Compose((10, 0, 5), quarter, (2, 2, 2));
model.TryDecompose(out var position, out var rotation, out var scale);   // true
position;                  // [10, 0, 5]
scale;                     // [2, 2, 2]
rotation.Angle(quarter);   // 0
```

A mirror image comes back as a negative `scale.X`. A matrix that isn't T R S gives `false`: one
with a shear (columns that aren't perpendicular), a zero scale, a projective last row, or an
entry that is NaN or infinite:

```csharp
Mat4.Scale((-1, 1, 1)).TryDecompose(out _, out _, out var mirrored);   // true, mirrored: [-1, 1, 1]
(Mat4.Identity with { M12 = 0.5 }).TryDecompose(out _, out _, out _);  // false: a shear
```

`Matrix3x3<T>` has the same pair for the plane, with an angle instead of a quaternion:

```csharp
var sprite = Matrix3x3<double>.Compose((100, 50), Math.PI / 6, (2, 2));
sprite.TryDecompose(out var at, out var angle, out var size);   // true
// at: [100, 50], angle: 0.5235987755982988 (pi / 6), size: [2, 2]
```

## Normals

A normal must stay perpendicular to its surface, which `TransformDirection` doesn't keep once
the scale differs between the axes. `TransformNormal` maps by the inverse transpose, which
`NormalMatrix()` gives once for many normals:

```csharp
var stretch = Mat4.Scale((2, 1, 1));
stretch.TransformNormal((1, -1, 0));      // [0.5, -1, 0]: the plane x = y, stretched, tilts
stretch.TransformDirection((1, -1, 0));   // [2, -1, 0]: no longer perpendicular to it
stretch.NormalMatrix();                   // [[0.5, 0, 0], [0, 1, 0], [0, 0, 1]]

Vec3[] normals = [(1, -1, 0), (0, 0, 1)];
stretch.TransformNormals(normals, normals);   // normals: [0.5, -1, 0], [0, 0, 1]
```

The mapped normals are not normalized; `Normalize()` them if the shading needs unit length.
