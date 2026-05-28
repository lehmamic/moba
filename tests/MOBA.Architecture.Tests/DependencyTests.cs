using ArchUnitNET.xUnitV3;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static MOBA.Architecture.Tests.MobaArchitecture;

namespace MOBA.Architecture.Tests;

/// <summary>
/// Compiler-enforces the dependency invariants pinned in
/// <c>docs/14-decision-log/adr-004-server-authoritative.md</c> and
/// <c>docs/14-decision-log/adr-005-project-structure.md</c>.
/// A failing test here means a load-bearing architectural rule has been broken.
/// </summary>
public class DependencyTests
{
    // ─── MOBA.Engine.Core — lowest layer, knows nothing else ──────────────────────────

    [Fact]
    public void EngineCore_does_not_depend_on_any_other_MOBA_assembly()
    {
        Types().That().ResideInAssembly(EngineCoreAssembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(
                    EngineGraphicsAssembly,
                    EngineNetworkingAssembly,
                    GameAssembly,
                    GameClientAssembly,
                    ServerAssembly,
                    ClientAssembly))
            .Because("Engine.Core is the foundation; it must not pull in higher layers.")
            .Check(Instance);
    }

    [Fact]
    public void EngineCore_does_not_depend_on_Silk_graphics_or_windowing_or_input()
    {
        Types().That().ResideInAssembly(EngineCoreAssembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(
                    SilkOpenGLAssembly,
                    SilkWindowingAssembly,
                    SilkInputAssembly))
            .Because("Engine.Core is renderer-, windowing-, and input-agnostic.")
            .Check(Instance);
    }

    // ─── MOBA.Engine.Graphics — Engine.Core only, plus Silk graphics ──────────────────

    [Fact]
    public void EngineGraphics_does_not_depend_on_higher_layers_or_other_engine_modules()
    {
        Types().That().ResideInAssembly(EngineGraphicsAssembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(
                    EngineNetworkingAssembly,
                    GameAssembly,
                    GameClientAssembly,
                    ServerAssembly,
                    ClientAssembly))
            .Because("Engine.Graphics is below Game and orthogonal to Networking.")
            .Check(Instance);
    }

    [Fact]
    public void EngineGraphics_does_not_depend_on_Windowing_or_Input()
    {
        // The OpenGL backend receives a GL context but does not own a window or read input.
        Types().That().ResideInAssembly(EngineGraphicsAssembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(
                    SilkWindowingAssembly,
                    SilkInputAssembly))
            .Because("Window + input ownership belongs to the entry-point project (MOBA.Client), not the backend.")
            .Check(Instance);
    }

    // ─── MOBA.Engine.Networking — pure leaf abstraction ───────────────────────────────

    [Fact]
    public void EngineNetworking_does_not_depend_on_any_other_MOBA_assembly()
    {
        Types().That().ResideInAssembly(EngineNetworkingAssembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(
                    EngineCoreAssembly,
                    EngineGraphicsAssembly,
                    GameAssembly,
                    GameClientAssembly,
                    ServerAssembly,
                    ClientAssembly))
            .Because("Networking is a leaf transport abstraction; it must stay unaware of Core/Game/Graphics.")
            .Check(Instance);
    }

    [Fact]
    public void EngineNetworking_does_not_depend_on_any_Silk_assembly()
    {
        Types().That().ResideInAssembly(EngineNetworkingAssembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(
                    SilkOpenGLAssembly,
                    SilkWindowingAssembly,
                    SilkInputAssembly))
            .Because("Networking has no business pulling Silk packages; the concrete transport (Riptide) lands later.")
            .Check(Instance);
    }

    // ─── MOBA.Game — sim, must run headless on the server ─────────────────────────────

    [Fact]
    public void Game_does_not_depend_on_EngineGraphics_or_GameClient()
    {
        Types().That().ResideInAssembly(GameAssembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(
                    EngineGraphicsAssembly,
                    GameClientAssembly,
                    ServerAssembly,
                    ClientAssembly))
            .Because("ADR-004: simulation is renderer-agnostic and must run headless on the server.")
            .Check(Instance);
    }

    [Fact]
    public void Game_does_not_depend_on_Silk_graphics_or_windowing_or_input()
    {
        Types().That().ResideInAssembly(GameAssembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(
                    SilkOpenGLAssembly,
                    SilkWindowingAssembly,
                    SilkInputAssembly))
            .Because("ADR-004: simulation is headless; no GPU/window/input deps allowed.")
            .Check(Instance);
    }

    // ─── MOBA.Game.Client — client-side rendering of sim state ────────────────────────

    [Fact]
    public void GameClient_does_not_depend_on_entry_points()
    {
        Types().That().ResideInAssembly(GameClientAssembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(
                    ServerAssembly,
                    ClientAssembly))
            .Because("Library code never depends on the entry-point assembly that hosts it.")
            .Check(Instance);
    }

    [Fact]
    public void GameClient_does_not_depend_on_Windowing()
    {
        Types().That().ResideInAssembly(GameClientAssembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(SilkWindowingAssembly))
            .Because("Window ownership belongs to MOBA.Client; Game.Client receives an IInputContext, not a window.")
            .Check(Instance);
    }

    // ─── MOBA.Server — headless entry point ───────────────────────────────────────────

    [Fact]
    public void Server_does_not_depend_on_EngineGraphics_or_GameClient_or_Client()
    {
        Types().That().ResideInAssembly(ServerAssembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(
                    EngineGraphicsAssembly,
                    GameClientAssembly,
                    ClientAssembly))
            .Because("ADR-004: the server build must not link any rendering or client-side code.")
            .Check(Instance);
    }

    [Fact]
    public void Server_does_not_depend_on_Silk_graphics_or_windowing_or_input()
    {
        Types().That().ResideInAssembly(ServerAssembly)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly(
                    SilkOpenGLAssembly,
                    SilkWindowingAssembly,
                    SilkInputAssembly))
            .Because("ADR-004: the server runs headless on machines without a GPU.")
            .Check(Instance);
    }
}
