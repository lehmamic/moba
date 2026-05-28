using ArchUnitNET.xUnitV3;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static MOBA.Architecture.Tests.MobaArchitecture;

namespace MOBA.Architecture.Tests;

/// <summary>
/// Visibility invariants — what is part of the public engine API vs. an implementation detail.
/// </summary>
public class VisibilityTests
{
    [Fact]
    public void OpenGL_resource_wrappers_are_not_public()
    {
        // Only OpenGLBackend is the public entry into the OpenGL implementation
        // (MOBA.Client constructs one). The Mesh/Texture/Shader wrappers are pure
        // implementation details — exposing them would leak GL state into the engine API
        // and break the Vulkan drop-in plan (ADR-003).
        Classes().That().ResideInNamespace("MOBA.Engine.Graphics.OpenGL")
            .And().DoNotHaveName("OpenGLBackend")
            .Should().NotBePublic()
            .Because("ADR-003: OpenGL resource wrappers are implementation details; the public API is the interfaces.")
            .Check(Instance);
    }
}
