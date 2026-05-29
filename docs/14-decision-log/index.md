# Decision Log

Architecture Decision Records following the [Nygard/Cognitect format](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions). See [adr-000-template.md](adr-000-template.md) for the template.

ADRs are immutable once accepted — never edit the Context, Decision, or Consequences of an existing record. If a decision changes, create a new ADR that supersedes the old one and update the old ADR's Status to "Superseded by ADR-NNN".

- [ADR-001: Right-handed Y-up coordinate system](adr-001-coordinate-system.md)
- [ADR-002: Silk.NET.Maths as math library](adr-002-math-library.md)
- [ADR-003: Graphics-backend abstraction with OpenGL as first backend](adr-003-graphics-backend-abstraction.md)
- [ADR-004: Server-authoritative architecture](adr-004-server-authoritative.md)
- [ADR-005: 7-project solution layout](adr-005-project-structure.md)
- [ADR-006: Riptide as networking library](adr-006-networking-riptide.md)
- [ADR-007: Idiomatic .NET / C# code style](adr-007-dotnet-idiomatic-style.md)
- [ADR-008: OpenGL backend lives in a sibling project](adr-008-opengl-backend-sibling-project.md)
- [ADR-009: GameHost shared between client and server](adr-009-gamehost-shared-abstraction.md)
- [ADR-010: One match per server process](adr-010-one-match-per-process.md)
- [ADR-011: Riptide concrete transport lives in a sibling project](adr-011-riptide-transport-sibling-project.md)
- [ADR-012: Classical Phong lighting with one directional light and Vertex-with-Normal layout](adr-012-phong-lighting-vertex-normals.md)
- [ADR-013: glTF 2.0 (.glb) as the model format, via SharpGLTF, with a project-owned shader registry](adr-013-gltf-model-format.md)

## Adding an ADR

When an architectural decision is made or changed:

1. Create a new file `adr-NNN-short-title.md` based on [adr-000-template.md](adr-000-template.md). `NNN` is the next free three-digit number.
2. Set Status to `Accepted` (or `Proposed` if still being discussed).
3. Add an entry to the list above.
4. If an old ADR is superseded: change its Status to `Superseded by [ADR-NNN](adr-NNN-title.md)`. **Nothing else** is edited on the old ADR — Context, Decision, and Consequences remain immutable.
