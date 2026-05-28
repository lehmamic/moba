namespace MOBA.Engine.Core;

public class Scene
{
    private readonly List<Actor> _actors = [];

    public IReadOnlyList<Actor> Actors => _actors;

    public void AddActor(Actor actor)
    {
        _actors.Add(actor);
        actor.OnBegin();
    }

    public void Update(GameTime time)
    {
        foreach (var a in _actors)
        {
            a.OnUpdate(time);
        }
    }

    public void Shutdown()
    {
        foreach (var a in _actors)
        {
            a.OnEnd();
        }
        _actors.Clear();
    }
}
