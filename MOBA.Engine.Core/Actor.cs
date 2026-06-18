namespace MOBA.Engine.Core;

public class Actor
{
    private readonly List<Component> _components = [];
    private readonly List<Actor> _children = [];

    public Actor() => Transform = new Transform(this);

    public Transform Transform { get; }

    public IReadOnlyList<Component> Components => _components;

    /// <summary>
    /// Logical hierarchy parent. Children are added via <see cref="AddChild"/>;
    /// the parent is set automatically. Scene-graph semantics: a child's world
    /// transform folds through its parent chain (see <see cref="Transform.World"/>)
    /// and <see cref="OnEnd"/> cascades through children, so removing a parent
    /// from the scene tears down the entire subtree.
    /// </summary>
    public Actor? Parent { get; private set; }

    public IReadOnlyList<Actor> Children => _children;

    /// <summary>
    /// Attaches <paramref name="child"/> to this actor's subtree. Throws if the
    /// child already has a parent — re-parenting is a deliberate operation and
    /// must go through <see cref="DetachFromParent"/> first.
    /// </summary>
    public void AddChild(Actor child)
    {
        if (child.Parent is not null)
        {
            throw new InvalidOperationException(
                $"Actor of type {child.GetType().Name} already has a parent ({child.Parent.GetType().Name}).");
        }
        child.Parent = this;
        _children.Add(child);
    }

    /// <summary>
    /// Returns the first direct child of type <typeparamref name="T"/>, or null
    /// if none exists. Symmetric to <see cref="GetComponent{T}"/> but for the
    /// hierarchy axis — useful for "one-of-a-kind subtree" lookups during
    /// scene construction (before the actor lands in <see cref="Scene"/>).
    /// </summary>
    public T? GetChild<T>() where T : Actor
    {
        foreach (var c in _children)
        {
            if (c is T match)
            {
                return match;
            }
        }
        return null;
    }

    /// <summary>Removes the link in both directions. Does not remove the child from the <see cref="Scene"/>.</summary>
    public void DetachFromParent()
    {
        if (Parent is null)
        {
            return;
        }
        Parent._children.Remove(this);
        Parent = null;
    }

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
        // Children first (deepest first when the cascade unwinds), then this
        // actor's own components — mirrors the ctor order in which child
        // subtrees were attached on top of this actor's own components.
        foreach (var child in _children)
        {
            child.OnEnd();
        }
        foreach (var c in _components)
        {
            c.OnEnd();
        }
    }
}
