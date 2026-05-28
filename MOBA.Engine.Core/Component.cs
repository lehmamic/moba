namespace MOBA.Engine.Core;

public abstract class Component
{
    protected Component(Actor owner)
    {
        Owner = owner;
        owner.AttachComponent(this);
    }

    public Actor Owner { get; }

    public virtual void OnBegin() { }

    public virtual void OnUpdate(GameTime time) { }

    public virtual void OnEnd() { }
}
