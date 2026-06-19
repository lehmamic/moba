using MOBA.Engine.Core.Hosting;
using MOBA.Engine.Core.Input;
namespace MOBA.Engine.Core.Abstractions;

/// <summary>
/// Per-match actor collection with a deferred-mutation policy: actors added
/// or removed while <see cref="Update"/> is iterating land in pending lists
/// and are flushed after the frame, so an actor's update can spawn or
/// despawn another actor without the change taking effect mid-iteration.
/// Madhav, <i>Game Programming in C++</i>, Ch. 2: <c>mUpdatingActors</c>
/// guard around <c>mActors</c> + <c>mPendingActors</c>; the removal side is
/// the same trick.
/// </summary>
public class Scene
{
    private readonly List<Actor> _actors = [];
    private readonly List<Actor> _pendingActors = [];
    private readonly List<Actor> _pendingRemovals = [];
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
    /// children (see <see cref="Actor.OnEnd"/>). When called from inside
    /// <see cref="Update"/> (e.g. a <c>DespawnOnDeathComponent</c> tick), the
    /// actor is queued and the real removal happens after the iteration
    /// finishes — so a component can safely ask the scene to drop its owner
    /// without invalidating the in-flight foreach.
    /// </summary>
    public bool RemoveActor(Actor actor)
    {
        if (_updating)
        {
            _pendingRemovals.Add(actor);
            return true;
        }

        return RemoveActorImmediate(actor);
    }

    private bool RemoveActorImmediate(Actor actor)
    {
        // Remove children from the flat list first, then the root — keeps the
        // invariant that any actor visible to the renderer always has its
        // parent visible too (no orphaned subtree mid-removal).
        foreach (var child in actor.Children.ToArray())
        {
            RemoveActorImmediate(child);
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

        // Apply removals first so a tick that despawns one actor and spawns
        // another doesn't leak the despawn into the next tick — the new
        // pending actor joins the live set on the same flush.
        if (_pendingRemovals.Count > 0)
        {
            foreach (var a in _pendingRemovals)
            {
                RemoveActorImmediate(a);
            }
            _pendingRemovals.Clear();
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
        _pendingRemovals.Clear();
    }
}
