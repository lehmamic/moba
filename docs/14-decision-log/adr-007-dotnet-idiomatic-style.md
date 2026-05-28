# ADR-007: Idiomatic .NET / C# code style — no 1:1 translations from C++

## Status

Accepted

## Context

Sanjay Madhav's *Game Programming in C++* is the conceptual learning reference for our engine (game loop, Actor/Component, renderer layering, math). His code style is classic AAA-engine-tradition C++: raw pointers (`Game*`, `Actor*`), global singletons (`Game::GetInstance()`), `new`/`delete`-based lifetime, header/`.cpp` splits, `std::vector`, `static_cast`, intrusive linked lists, macros.

Many online tutorials for C++ game engines are similar or even hackier ("static everything", god objects, manual vtable tricks). A naive translation into C# produces code that looks like bad C++ — not like idiomatic .NET.

We want code that looks like Microsoft writes in `dotnet/runtime`, `dotnet/aspnetcore`, and MAUI.

## Decision

**Idiomatic .NET / C# code style is binding.** Madhav's code is a learning reference for **concepts**, not a translation template for **code form**. Concrete substitutions:

| C++ / book | .NET idiom |
|---|---|
| `new`/`delete`, manual lifetime | GC; `IDisposable` + `using` **only** for unmanaged resources (GL handles, native pointers) |
| Raw pointers (`Actor*`, `Game*`) | reference fields injected via constructor |
| Header/`.cpp` split | one `.cs` per type; **no** artificial `partial class` splits |
| Global singletons (`Game::GetInstance()`) | constructor injection; minimal static state |
| `#include` guards, forward declarations | `using` directives, namespaces |
| `std::vector<T>` | `List<T>` or `T[]`; `Span<T>` for hot paths |
| `std::unique_ptr` / `std::shared_ptr` | plain references |
| `static_cast<T>` | `(T)x` for value conversion, `x as T` for nullable reference cast, pattern matching where it reads better |
| `enum class` | `enum` |
| Intrusive linked lists | `List<T>` / `LinkedList<T>` / `Dictionary<,>` |
| Macros | generics, attributes, source generators |
| `friend class` | `internal` + `[InternalsVisibleTo]` if absolutely necessary |
| Multiple inheritance | composition + interfaces; default-interface methods used sparingly |
| Public mutable structs | `readonly record struct` for value semantics; otherwise class |
| Manual `virtual` vtables | `interface` + `abstract`/`virtual` straightforwardly |

**Style conventions** (enforced via `.editorconfig` + analyzers):

- File-scoped namespaces (`namespace MOBA.X;`)
- PascalCase types/methods/properties/events; camelCase parameters/locals; `_camelCase` private/internal fields (Microsoft runtime convention); `s_camelCase` private static
- `I`-prefix interfaces, `T`-prefix type parameters
- `nullable enable` project-wide
- `var` when the right-hand-side type is obvious, otherwise explicit type
- Allman braces
- `async`/`await` with `Async` method-name suffix
- `readonly` wherever possible
- Properties instead of public fields
- Pattern matching instead of cast chains
- Records for DTOs / net messages
- `using` directives: `System.*` first, sorted
- `TreatWarningsAsErrors=true` + `EnforceCodeStyleInBuild=true` — style violations break the build.

**Concrete book-translation patterns** for the engine:

- `Game` class: **no** global instance. It is constructed in `Program.cs`; if other classes need it, it is passed via constructor.
- `Actor` / `Component`: the owner reference is passed to the component constructor and registered through `Actor.AttachComponent`. No raw pointer trickery.
- GL resource wrappers (`OpenGLTexture`, `OpenGLShader`, `OpenGLMesh`) implement `IDisposable`. The client disposes them deterministically on window close.
- Asset loading: no static `LoadTexture(path)` singleton call like in the book. Backend `CreateTexture` factory + a future `IAssetCache` injected.
- Math: no custom `Vector3`/`Matrix4` class — use the Silk.NET.Maths types directly. Book listings like "Listing 5.6" collapse to a single line `Matrix4X4.CreatePerspectiveFieldOfView(...)`.

## Consequences

- **Positive:** the code reads naturally for anyone with .NET experience. Style is tooling-enforced (no bikeshedding). Onboarding via Microsoft's code examples is feasible.
- **Negative:** a bit more effort when translating from the book — we cannot "just retype". Worth it.
- **Deviations:** only on **explicit user request** (e.g. a perf-critical hot path that justifies `unsafe`). When the idiomatic way is unclear → **ask**, do not guess.
- **Suppressions globally** (in `.editorconfig`): CA1859 ("use concrete type for perf") disabled — it fights the `IGraphicsBackend` abstraction. CA1707 (underscores in identifiers) because of `_camelCase`. CS1591 (missing XML doc) because doc comments are optional.
