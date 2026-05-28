# ADR-004: Server-authoritative architecture

## Status

Accepted

## Context

MOBAs are competitive multiplayer titles. Every established game in the genre (LoL, Dota 2, HotS) is built on a server-authoritative model: the server computes the truth, the client renders an interpolated view. This removes entire classes of cheats (speed hacks, stat hacks, map hacks for vision the player should not have) physically, because the client never sees or can overwrite the data.

The alternative (peer-to-peer, lockstep, deterministic) works for classic RTS games (StarCraft, AoE), but for action MOBAs it is too inflexible — reconnect, spectator, replay are all harder, and determinism bugs become a constant plague.

## Decision

We build **server-authoritative**:

- **Server** runs headless and simulates at a fixed tick rate (skeleton default: 30 Hz). Pathfinding (DotRecast), movement, abilities, damage, cooldowns, vision/fog-of-war live there.
- **Client** sends inputs ("move to (x,y)", "cast Q on target") and renders received snapshots — rendering happens ~100 ms behind server time, interpolated in between ("entity interpolation").
- **The local player's champion** can optionally do client-side prediction of movement inputs (with server reconciliation on divergence). Not implemented in the first skeleton slice.
- **Fog-of-war filtering on the server:** the client only receives state inside its vision.
- **No lockstep determinism:** state replication tolerates float indeterminism, server state is always the truth.

Structural consequence for the engine — the sim/render separation is a **hard invariant**:

- `MOBA.Game` (simulation) may **never** reference `MOBA.Engine.Graphics` or any Silk graphics package.
- `MOBA.Server` (headless entry point) may **never** reference `Silk.NET.Windowing`, `Silk.NET.OpenGL`, `Silk.NET.Input`.

Violation = immediate refactor. To be backed by a CI sanity check later.

## Consequences

- **Positive:** cheat-resistant architecture from day 1, industry-standard MOBA pattern, the engine structure enforces sim/render separation from the start, headless server testing without a GPU.
- **Negative:** more complexity than a single-process setup. In the first skeleton slice we side-step it with `NullTransport` (in-process loopback): the server code path exists, but for the skeleton visual check the sim runs directly inside the client process.
- **Out of scope for the skeleton:** client-side prediction, reconciliation, entity interpolation, snapshot delta compression. These arrive with the real netcode (ADR-006, Riptide).
