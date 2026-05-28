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

    /// <summary>
    /// Per-frame input driver. Calls <see cref="ProcessInputComponents"/> first
    /// (each component sees the snapshot) then the virtual subclass hook
    /// <see cref="ActorInput"/>. Like <see cref="OnUpdate"/> this is intentionally
    /// non-virtual.
    /// </summary>
    public void ProcessInput(InputState state)
    {
        ProcessInputComponents(state);
        ActorInput(state);
    }

    /// <summary>Routes the input snapshot to every component.</summary>
    protected void ProcessInputComponents(InputState state)
    {
        foreach (var c in _components)
        {
            c.OnProcessInput(state);
        }
    }

    /// <summary>Override to react to per-frame input beyond what components do.</summary>
    protected virtual void ActorInput(InputState state)
    {
    }

    /// <summary>
    /// Per-tick driver. Calls <see cref="UpdateComponents"/> first so subclass logic
    /// in <see cref="UpdateActor"/> sees the state components have already produced.
    /// This is intentionally non-virtual; subclasses override <see cref="UpdateActor"/>
    /// instead of <see cref="OnUpdate"/> and never need to remember to call base.
    /// (Madhav, <i>Game Programming in C++</i>, Ch. 2: <c>Actor::Update</c> →
    /// <c>UpdateComponents</c> + <c>UpdateActor</c>.)
    /// </summary>
    public void OnUpdate(GameTime time)
    {
        UpdateComponents(time);
        UpdateActor(time);
    }

    /// <summary>Iterates attached components and ticks each one.</summary>
    protected void UpdateComponents(GameTime time)
    {
        foreach (var c in _components)
        {
            c.OnUpdate(time);
        }
    }

    /// <summary>Override to add actor-specific per-tick behaviour beyond what components do.</summary>
    protected virtual void UpdateActor(GameTime time)
    {
    }

    public virtual void OnEnd()
    {
        foreach (var c in _components)
        {
            c.OnEnd();
        }
    }
}
