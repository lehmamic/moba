using MOBA.Engine.Core.Abstractions;

namespace MOBA.Engine.Graphics.Rendering;

/// <summary>
/// Wraps a <see cref="Camera"/> as an <see cref="Actor"/> so the controller
/// that drives the camera (orbit / free-fly / scripted cinematic) can live
/// as a <c>Component</c> on it — same pattern as every other actor in the
/// engine. The viewport hands <see cref="Camera"/> to the renderer each
/// frame; the camera-controller component updates Position / Target / Up on
/// the same instance via <c>OnProcessInput</c> / <c>OnUpdate</c>.
/// </summary>
public sealed class CameraActor : Actor
{
    public CameraActor() => Camera = new Camera();

    public Camera Camera { get; }
}
