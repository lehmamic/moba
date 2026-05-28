# ADR-010: One match per server process

## Status

Accepted

## Context

A MOBA server has to host many concurrent matches in production. Two architectural patterns exist:

1. **Multiplex many matches per process** — a single OS process runs N `GameHost` instances (typically one Task per match) with shared address space, shared engine code, and one set of allocator pages.
2. **One match per process** — every match is its own OS process; concurrent matches mean concurrent processes scaled by a container orchestrator (Docker / Kubernetes / Nomad) or a unit manager (systemd / launchd).

We had to pick one as the engine's deployment shape because it changes whether a `GameServer`-style in-process orchestrator (option 1) is part of the architecture or not.

## Decision

**One match per OS process.** The server entry point (`MOBA.Server/Program.cs`) constructs **exactly one** `ServerGame` per `dotnet run` (or container start) and the `ServerGame.Run()` loop is the process's main loop. There is **no** in-process orchestrator class hosting a list of matches; that responsibility belongs to the deployment layer (Docker / k8s / systemd / a launcher script).

## Reasoning

- **Crash isolation.** An unhandled exception in one match cannot take down the others. With multiplexed matches a single bad tick aborts the whole process.
- **Resource accounting.** Per-match CPU, RSS, file descriptors, and GC pauses are observable through standard OS metrics without any custom instrumentation. With multiplexed matches you have to attribute resources yourself.
- **Hot restart.** Restart, upgrade, or migrate one match without disturbing the rest. The orchestrator handles this transparently.
- **Industry precedent.** Riot (League of Legends), Valve (Dota 2), and Blizzard (Heroes of the Storm) all run one match per server process scaled via container orchestration. Their architecture is the proof of viability at scale.
- **Engine simplicity.** Inside the engine there is no shared state between matches, no thread-safety requirement on the sim, no cross-match resource contention, no shutdown-ordering between matches.
- **Local development.** Multi-match testing remains trivial: open multiple terminals, run `dotnet run --project MOBA.Server` in each. Each instance is independent.

## Consequences

- **Positive:**
  - `ServerGame` owns its own `Run()` and its own loop. No `GameServer` class in the codebase; nothing to refactor when scaling.
  - The server build pulls no thread-pool or scheduling primitives beyond a single `CancellationTokenSource` for Ctrl-C handling.
  - Scaling becomes an operations concern (deployment manifests), explicitly *outside* the engine repository's responsibility.
- **Negative:**
  - Per-process overhead: each match pays for one .NET runtime, one JIT pass, one address-space worth of dirty pages. In production this is acceptable; container orchestration is designed for exactly this shape.
  - "Run 10 matches locally for a load test" is one `for` loop in a shell script rather than one `AddMatch` call. Acceptable.
- **Non-consequence for ADR-009.** The shared `GameHost` abstraction is still the right shape — it represents one session (= one match on the server, = one client connection on the client). Hosting many sessions per process is just not part of our model.
