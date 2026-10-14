# Camera

To draw a point of a model, the model matrix takes it into the world, the view matrix into the
space of the camera, and the projection onto the screen: `projection * view * model`. This
page builds the view and the projection. The examples use the aliases of
[Fixed-size vectors and matrices](fixed-size.md#entry-types):

```csharp
using Vec3 = Epsilon.LinearAlgebra.Vector3<double>;
using Mat4 = Epsilon.LinearAlgebra.Matrix4x4<double>;
```

## View and perspective

The view and the projections are right-handed, with the camera looking along -z, x to the
right and y up, as in OpenGL and glTF; each has a `LeftHanded` twin that looks along +z, as in
Direct3D:

```csharp
var view = Mat4.LookAt((0, 2, 5), (0, 0, 0), Vec3.UnitY);    // eye, target, up
view.TransformPoint((0, 2, 5));                              // [0, 0, 0]: the eye
var projection = Mat4.Perspective(Math.PI / 3, 16.0 / 9, 0.1, 100);
projection.TransformPoint((0, 0, -0.1));                     // [0, 0, 0]: the near plane
(projection * view).TransformPoint((0, 0, 0)).Z;             // 0.9824128790613561: the depth of the target

var leftHanded = Mat4.PerspectiveLeftHanded(Math.PI / 3, 16.0 / 9, 0.1, 100);
leftHanded.TransformPoint((0, 0, 0.1));                      // [0, 0, 0]: the near plane at +z
```

`LookAt` throws `ArgumentException` when the target is the eye, or when the up vector is
parallel to the direction of view, as there is then no way to tell up from down.

`Perspective` takes the vertical angle of view in radians, the width over the height, and the
distances of the near and the far plane; the far one may be `PositiveInfinity`. What the camera
sees lands in x and y from -1 to 1, after `TransformPoint` divides by w, and at the depths of a
`DepthRange`:

| `DepthRange` | Near, far | For |
|---|---|---|
| `ZeroToOne` | 0, 1 | Direct3D, Vulkan, Metal, WebGPU; the default |
| `MinusOneToOne` | -1, 1 | OpenGL, WebGL |
| `OneToZero` | 1, 0 | Reverse-Z: a float depth buffer then resolves the distance best |

```csharp
var openGL = Mat4.Perspective(Math.PI / 3, 16.0 / 9, 0.1, 100, DepthRange.MinusOneToOne);
openGL.TransformPoint((0, 0, -100)).Z;      // 1: the far plane

var reversed = Mat4.Perspective(Math.PI / 3, 16.0 / 9, 0.1, double.PositiveInfinity, DepthRange.OneToZero);
reversed.TransformPoint((0, 0, -1000)).Z;   // 0.0001: near / distance, with no far plane
```

## Orthographic projection

`Orthographic(width, height, near, far)` projects without perspective, for drawings, isometric
games and the shadows of the sun; `OrthographicOffCenter` takes any box, such as the pixels of
the screen:

```csharp
var map = Mat4.Orthographic(20, 10, 0.1, 100);
map.TransformPoint((10, 5, -0.1));    // [1, 1, 0]: the top right corner of the near plane

var ui = Mat4.OrthographicOffCenter(0, 1920, 1080, 0, -1, 1);
ui.TransformPoint((0, 0, 0));         // [-1, 1, 0.5]: the top left pixel at the top left corner
ui.TransformPoint((1920, 1080, 0));   // [1, -1, 0.5]
```

## A first-person camera

With these a first-person camera that the mouse turns takes a few lines; the window, the input
and the drawing come from a graphics library:

```csharp
yaw -= mouseDeltaX * sensitivity;                  // the mouse to the right turns right
pitch = Math.Clamp(pitch - mouseDeltaY * sensitivity, -1.55, 1.55);   // never straight up
var orientation = Quaternion<double>.RotationY(yaw) * Quaternion<double>.RotationX(pitch);
Vec3 forward = orientation.Rotate(-Vec3.UnitZ), right = orientation.Rotate(Vec3.UnitX);
position += (forward * moveForward + right * moveRight) * (speed * deltaTime);

var viewProjection = Mat4.Perspective(Math.PI / 3, aspect, 0.1, 1000)
                   * Mat4.LookAt(position, position + forward, Vec3.UnitY);
System.Numerics.Matrix4x4 forTheGpu = viewProjection.Map(float.CreateTruncating).ToSystemNumerics();
```

The simulation stays in `double`, where positions far from the origin keep their precision,
and only the final matrix becomes `float`. The memory of a `System.Numerics.Matrix4x4` is this
matrix column by column, which is what a GLSL shader computing `viewProjection * position`
expects.

## An orbit camera

A camera that circles a target, as in a model viewer, turns the offset from the target instead
of the direction of view:

```csharp
double yaw = Math.PI / 2, pitch = 0.5, distance = 10;
Vec3 target = (0, 1, 0);
var orbit = Quaternion<double>.RotationY(yaw) * Quaternion<double>.RotationX(-pitch);
Vec3 eye = target + orbit.Rotate((0, 0, distance));   // above the target, 10 away
Mat4.LookAt(eye, target, Vec3.UnitY).TransformPoint(target);   // [0, 0, -10]: straight ahead
```

## Picking with the mouse

The inverse of `projection * view` takes a point of the screen back into the world. A pixel
gives a ray from the near plane to the far one, which can then be tested against the scene:

```csharp
var viewProjection = projection * view;
double ndcX = 2 * mouseX / width - 1, ndcY = 1 - 2 * mouseY / height;   // pixels to [-1, 1]
var unproject = viewProjection.Inverse();
Vec3 near = unproject.TransformPoint((ndcX, ndcY, 0)), far = unproject.TransformPoint((ndcX, ndcY, 1));
Vec3 ray = (far - near).Normalize();

// Where the ray meets the ground, the plane y = 0:
Vec3 hit = near + (-near.Y / ray.Y) * ray;
```

The depths 0 and 1 are those of the default `DepthRange.ZeroToOne`. With the camera above, the
pixel in the middle of a 1920 x 1080 screen hits the target at the origin, up to rounding; the
bottom right one hits the ground at about (2.26, 0, 3.43).
