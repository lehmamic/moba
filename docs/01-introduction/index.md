# Introduction

`dotnet-moba` is a custom Multiplayer Online Battle Arena (MOBA) game built on a self-made game engine in C#/.NET 10. The engine takes its conceptual cues from Sanjay Madhav's *Game Programming in C++* — game loop, Actor/Component pattern, layered renderer, math foundations — but is written in idiomatic .NET, not as a 1:1 translation of the book's C++ code.

This guidebook follows Simon Brown's [Software Guidebook](https://leanpub.com/documenting-software-architecture) outline. It captures what the source code alone cannot: context, architectural drivers, constraints, principles, decision rationale. ADRs in [section 14](../14-decision-log/index.md) are the source of truth for any architectural decision; the earlier sections summarise and link.

The single most important architectural property is **server-authoritative simulation** with a clean separation between simulation and rendering — see [Quality Attributes](../04-quality-attributes/index.md) and [ADR-004](../14-decision-log/adr-004-server-authoritative.md).
