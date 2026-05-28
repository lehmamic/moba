# AGENTS.md — Working Rules for Coding Agents

This document holds the **agent-specific working rules** for the `dotnet-moba` repo: how I communicate, where I put plans, where I find architectural decisions. Project architecture and engineering principles live in the [Software Guidebook](docs/index.md) — this file references it, it does not duplicate it.

---

## 1. Language

All written artifacts in this repo are **English only**: source-code comments, XML docs, ADRs, Software Guidebook sections, AGENTS.md, plan files, console-output strings, configuration-file comments. The user converses in German (and German replies in chat are fine) but anything that lives in the repo is English-only.

---

## 2. Plans

Plan-mode plans are initially written by the harness to `~/.claude/plans/`. **As soon as the plan is approved (or as the very first implementation action):** copy the plan content into the repo at `./.plans/YYYY-MM-DD-short-title.md`.

- Format: `YYYY-MM-DD-short-title.md`. The date is the day the plan was approved.
- Plans are **immutable** — a new iteration creates a new dated file alongside, the old one stays as history.
- `.plans/` is **gitignored** (see `.gitignore`) — local-only working-copy history, not shared via the repo.

---

## 3. Where to find what

Before any non-trivial change, consult the relevant Software Guidebook section:

| Topic | Section |
|---|---|
| Project pitch, audience, outline of this guidebook | [01 — Introduction](docs/01-introduction/index.md) |
| What we optimise for (cheat resistance, portability, build-time enforcement) | [04 — Quality Attributes](docs/04-quality-attributes/index.md) |
| Fixed inputs (.NET 10, platforms, libraries, learning reference) | [05 — Constraints](docs/05-constraints/index.md) |
| Coordinate & math conventions, .NET-idiomatic style, sim/render separation | [06 — Principles](docs/06-principles/index.md) |
| 7-project layout, server-authoritative model, graphics/networking abstractions | [07 — Software Architecture](docs/07-software-architecture/index.md) |
| Repo layout, style conventions, suppressed analyzers, configuration files | [08 — Code](docs/08-code/index.md) |
| How to build and run, IDE setup, client controls | [13 — Development Environment](docs/13-development-environment/index.md) |
| ADRs (immutable rationale) + how to add a new one | [14 — Decision Log](docs/14-decision-log/index.md) |

ADRs are the immutable source of truth for any architectural decision. The earlier guidebook sections summarise and link.

---

## 4. When changing things

- **Code change that fits an existing principle / decision:** just make the change. The compiler + `.editorconfig` + `Directory.Build.props` will keep style and dependency invariants honest. ArchUnitNET tests in `tests/MOBA.Architecture.Tests/` catch dependency / naming / visibility violations at `dotnet test` time — run them locally before committing a non-trivial change.
- **Code change that touches a principle or invariant:** read the linked ADR first. If the change deviates, you need a new ADR (see [Decision Log — Adding an ADR](docs/14-decision-log/index.md#adding-an-adr)) that supersedes the old one. Do not silently change a load-bearing rule. If the new rule needs a new architecture test, add it next to the existing ones.
- **Architectural change** (a new library, a new abstraction, a new project boundary): write a new ADR, update the affected guidebook section(s), then implement.
- **When the idiomatic path is not obvious:** ask, do not guess. See [Principles — Idiomatic .NET / C# style](docs/06-principles/index.md#2-idiomatic-net--c-style).

---

## 5. Commits

### Single responsibility

**One commit = one purpose.** If a change does several unrelated things (e.g. an infrastructure cleanup plus a feature, or two unrelated refactors), split it into multiple commits. Each commit's diff has to be reviewable on its own and revertible without ripping out unrelated work.

When a single feature legitimately touches many files (a new project, a new architectural layer, a guidebook restructure) it is still one purpose — that's fine as one commit.

### Conventional Commits

Use the [Conventional Commits 1.0](https://www.conventionalcommits.org/en/v1.0.0/) format:

```
<type>(<scope>): <short summary>

<optional body explaining the why>
```

**Allowed types:**

| Type | When |
|---|---|
| `feat` | new user-visible or developer-visible feature |
| `fix` | bug fix |
| `refactor` | code restructuring without behavioural change |
| `docs` | documentation only (`docs/`, `AGENTS.md`, XML docs) |
| `test` | adding or fixing tests |
| `build` | build infrastructure (`Directory.*.props`, `.editorconfig`, `.slnx` structure, csproj boilerplate) |
| `deps` | NuGet version bumps in `Directory.Packages.props` |
| `chore` | repo hygiene that does not fit elsewhere |
| `perf` | performance change |
| `style` | pure formatting (rare — we enforce style at build) |
| `ci` | continuous-integration configuration |
| `revert` | reverts an earlier commit |

**Allowed scopes** (project-aligned; this is where the scopes are *defined* — keep this table in sync as the layout evolves):

| Scope | Covers |
|---|---|
| `engine-core` | `MOBA.Engine.Core` |
| `engine-graphics` | `MOBA.Engine.Graphics` incl. the OpenGL backend |
| `engine-networking` | `MOBA.Engine.Networking` |
| `game` | `MOBA.Game` |
| `game-client` | `MOBA.Game.Client` |
| `server` | `MOBA.Server` |
| `client` | `MOBA.Client` |
| `tests` | anything under `tests/` |
| `assets` | anything under `assets/` |
| `guidebook` | anything under `docs/` (sections 01–13) |
| `adr` | anything under `docs/14-decision-log/` |
| `agents` | `AGENTS.md` itself |

Omit the scope (`<type>: <summary>`) when the change spans many scopes or does not fit any single one (e.g. a cross-cutting style sweep, repo-wide build infrastructure). Do not invent new scopes — extend this table first.

**Examples:**

```
feat(game-client): add free-fly camera controller
fix(engine-graphics): correct CCW winding on cube -Y face
docs(engine-core): clarify Game.Tick contract
test(tests): add naming check for sim components
build: centralize NuGet versions via Directory.Packages.props
deps: bump Silk.NET to 2.24.0
```

### Commit-message rules

- **Subject line:** ≤ 70 characters, imperative mood ("add", "fix", "switch to"), no trailing period.
- **Body:** wrapped at ~72 columns, explains the *why* — what motivated the change. Skip the body for truly trivial commits.
- **No agent attribution.** Do not add `Co-Authored-By: Claude` or similar trailers — the git author is the author.
- **No emoji.**
