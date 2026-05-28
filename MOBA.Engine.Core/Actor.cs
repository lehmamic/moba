using Silk.NET.Maths;

namespace MOBA.Engine.Core;

public class Actor
{
    private readonly List<Component> _components = [];

    public Vector3D<float> Position { get; set; } = Vector3D<float>.Zero;

    public Quaternion<float> Rotation { get; set; } = Quaternion<float>.Identity;

    public Vector3D<float> Scale { get; set; } = Vector3D<float>.One;

    public IReadOnlyList<Component> Components => _components;

    public Matrix4X4<float> WorldMatrix =>
        Matrix4X4.CreateScale(Scale)
        * Matrix4X4.CreateFromQuaternion(Rotation)
        * Matrix4X4.CreateTranslation(Position);

    internal void AttachComponent(Component component) => _components.Add(component);

    public T? GetComponent<T>() where T : Component
    {
        foreach (var c in _components)
        {
            if (c is T match)
            {
                return match;
            }
        }
        return null;
    }

    public virtual void OnBegin()
    {
        foreach (var c in _components)
        {
            c.OnBegin();
        }
    }

    public virtual void OnUpdate(GameTime time)
    {
        foreach (var c in _components)
        {
            c.OnUpdate(time);
        }
    }

    public virtual void OnEnd()
    {
        foreach (var c in _components)
        {
            c.OnEnd();
        }
    }
}
