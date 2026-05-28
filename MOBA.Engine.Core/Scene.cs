namespace MOBA.Engine.Core;

/// <summary>
/// Per-match actor collection with a deferred-add policy: actors added while
/// <see cref="Update"/> is iterating land in a pending list and join the live
/// set after the frame, so an actor's update can spawn another actor without
/// the new one being ticked in the same frame (and without mutating the list
/// mid-iteration). Madhav, <i>Game Programming in C++</i>, Ch. 2:
/// <c>mUpdatingActors</c> guard around <c>mActors</c> + <c>mPendingActors</c>.
/// </summary>
public class Scene
{
    private readonly List<Actor> _actors = [];
    private readonly List<Actor> _pendingActors = [];
    private bool _updating;

    public IReadOnlyList<Actor> Actors => _actors;

    public void AddActor(Actor actor)
    {
        if (_updating)
        {
            _pendingActors.Add(actor);
        }
        else
        {
            _actors.Add(actor);
        }
        actor.OnBegin();
    }

    public bool RemoveActor(Actor actor)
    {
        if (_actors.Remove(actor))
        {
            actor.OnEnd();
            return true;
        }
        if (_pendingActors.Remove(actor))
        {
            actor.OnEnd();
            return true;
        }
        return false;
    }

    public void ProcessInput(InputState state)
    {
        _updating = true;
        try
        {
            foreach (var a in _actors)
            {
                a.ProcessInput(state);
            }
        }
        finally
        {
            _updating = false;
        }
        // Pending actors stay pending until the next Update flushes them, so
        // ProcessInput and Update operate on the same active set within one frame.
    }

    public void Update(GameTime time)
    {
        _updating = true;
        try
        {
            foreach (var a in _actors)
            {
                a.OnUpdate(time);
            }
        }
        finally
        {
            _updating = false;
        }

        if (_pendingActors.Count > 0)
        {
            _actors.AddRange(_pendingActors);
            _pendingActors.Clear();
        }
    }

    public void Shutdown()
    {
        foreach (var a in _actors)
        {
            a.OnEnd();
        }
        foreach (var a in _pendingActors)
        {
            a.OnEnd();
        }
        _actors.Clear();
        _pendingActors.Clear();
    }
}
