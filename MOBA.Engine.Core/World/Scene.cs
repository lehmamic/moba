using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Core.Input;
namespace MOBA.Engine.Core.World;

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

    /// <summary>
    /// Returns the first actor of type <typeparamref name="T"/> in the scene,
    /// or null if none exists. Convenience for one-of-a-kind actors (e.g. the
    /// <c>MapActor</c>, the local <c>PlayerActor</c>) without forcing every
    /// caller to spell out the LINQ <c>.OfType&lt;T&gt;().FirstOrDefault()</c>.
    /// </summary>
    public T? GetActor<T>() where T : Actor
    {
        foreach (var a in _actors)
        {
            if (a is T match)
            {
                return match;
            }
        }
        return null;
    }

    /// <summary>
    /// Adds <paramref name="actor"/> and every actor in its subtree (via
    /// <see cref="Actor.Children"/>) to the flat actor list — Renderer
    /// iteration stays flat regardless of how deep the hierarchy is.
    /// <see cref="Actor.OnBegin"/> is called per actor in subtree order.
    /// </summary>
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
        foreach (var child in actor.Children)
        {
            AddActor(child);
        }
    }

    /// <summary>
    /// Removes <paramref name="actor"/> and every actor in its subtree. Calls
    /// <see cref="Actor.OnEnd"/> once at the root, which cascades through
    /// children (see <see cref="Actor.OnEnd"/>).
    /// </summary>
    public bool RemoveActor(Actor actor)
    {
        // Remove children from the flat list first, then the root — keeps the
        // invariant that any actor visible to the renderer always has its
        // parent visible too (no orphaned subtree mid-removal).
        foreach (var child in actor.Children.ToArray())
        {
            RemoveActor(child);
        }
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

    public void Shutdown() => Clear();

    /// <summary>
    /// Ends every actor and empties both lists. Used both by host shutdown
    /// and by <c>GameHost.LoadScene</c> when swapping to a new scene — this
    /// is a hard reset, networked actors that are not reborn by the next
    /// scene's load step will be gone after this returns.
    /// </summary>
    public void Clear()
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
