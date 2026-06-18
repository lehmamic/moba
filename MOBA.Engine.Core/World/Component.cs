using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Core.Input;
namespace MOBA.Engine.Core.World;

public abstract class Component
{
    protected Component(Actor owner)
    {
        Owner = owner;
        owner.AttachComponent(this);
    }

    public Actor Owner { get; }

    public virtual void OnBegin() { }

    /// <summary>
    /// Optional input hook called once per frame, before <see cref="OnUpdate"/>,
    /// with the captured <see cref="InputState"/> snapshot. Components own the
    /// decision of what to do with input.
    /// </summary>
    public virtual void OnProcessInput(InputState state) { }

    public virtual void OnUpdate(GameTime time) { }

    public virtual void OnEnd() { }
}
