namespace MOBA.Engine.Core;

public class Actor
{
    private readonly List<Component> _components = [];

    public Transform Transform { get; } = new();

    public IReadOnlyList<Component> Components => _components;

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
