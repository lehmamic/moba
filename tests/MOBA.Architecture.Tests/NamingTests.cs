using ArchUnitNET.xUnitV3;
using MOBA.Engine.Core;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static MOBA.Architecture.Tests.MobaArchitecture;

namespace MOBA.Architecture.Tests;

/// <summary>
/// Naming conventions from <c>docs/06-principles/index.md</c> and
/// <c>docs/14-decision-log/adr-007-dotnet-idiomatic-style.md</c>.
/// </summary>
public class NamingTests
{
    [Fact]
    public void Interfaces_in_MOBA_assemblies_start_with_I()
    {
        Interfaces().That().ResideInAssembly(
                EngineCoreAssembly,
                EngineGraphicsAssembly,
                EngineNetworkingAssembly,
                GameAssembly,
                GameClientAssembly)
            .Should().HaveNameStartingWith("I")
            .Because("Standard .NET convention; enforced project-wide.")
            .Check(Instance);
    }

    [Fact]
    public void Sim_components_in_MOBA_Game_end_with_Component()
    {
        Classes().That().ResideInAssembly(GameAssembly)
            .And().AreAssignableTo(typeof(Component))
            .And().AreNotAbstract()
            .Should().HaveNameEndingWith("Component")
            .Because("ADR-005 + AGENTS.md: sim components use the `<Name>Component` naming pattern.")
            .Check(Instance);
    }

    [Fact]
    public void Client_only_components_in_MOBA_Game_Client_use_a_distinguishing_suffix()
    {
        // Client-only components live only in Game.Client and have a distinctive suffix
        // that makes the concern obvious at the call site:
        //   RendererComponent / VisualComponent — render-side
        //   InputComponent                       — local-player input → server-bound commands
        Classes().That().ResideInAssembly(GameClientAssembly)
            .And().AreAssignableTo(typeof(Component))
            .And().AreNotAbstract()
            .Should().HaveNameEndingWith("RendererComponent")
            .OrShould().HaveNameEndingWith("VisualComponent")
            .OrShould().HaveNameEndingWith("InputComponent")
            .Because("docs/06-principles + AGENTS.md: client-only components are suffixed by concern (RendererComponent / VisualComponent for render, InputComponent for local input).")
            .Check(Instance);
    }
}
