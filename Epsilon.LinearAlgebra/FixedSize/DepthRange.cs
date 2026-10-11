namespace Epsilon.LinearAlgebra;

/// <summary>
/// The depths that a projection gives the near and the far plane, after the division by w: the
/// convention of the graphics API that draws the result.
/// </summary>
public enum DepthRange
{
    /// <summary>Near 0, far 1: Direct3D, Vulkan, Metal and WebGPU.</summary>
    ZeroToOne,

    /// <summary>Near -1, far 1: OpenGL and WebGL.</summary>
    MinusOneToOne,

    /// <summary>
    /// Near 1, far 0: reverse-Z, for a depth buffer of floats, whose fine resolution near 0 then
    /// goes to the distance, where the projection needs it.
    /// </summary>
    OneToZero,
}
