using System.Reflection;
using ArchUnitNET.Loader;

namespace MOBA.Architecture.Tests;

/// <summary>
/// Pre-loaded ArchUnit <see cref="ArchUnitNET.Domain.Architecture"/> over every MOBA.* and
/// Silk.NET.* assembly the dependency rules care about. <see cref="Assembly.Load(string)"/>
/// works because the test project takes a project reference to every production project,
/// so each .dll ends up in the test output directory.
/// </summary>
public static class MobaArchitecture
{
    public static readonly Assembly UtilitiesAssembly = Assembly.Load("MOBA.Utilities");
    public static readonly Assembly EngineCoreAssembly = Assembly.Load("MOBA.Engine.Core");
    public static readonly Assembly EngineGraphicsAssembly = Assembly.Load("MOBA.Engine.Graphics");
    public static readonly Assembly EngineGraphicsOpenGLAssembly = Assembly.Load("MOBA.Engine.Graphics.OpenGL");
    public static readonly Assembly EngineNetworkingAssembly = Assembly.Load("MOBA.Engine.Networking");
    public static readonly Assembly GameAssembly = Assembly.Load("MOBA.Game");
    public static readonly Assembly GameClientAssembly = Assembly.Load("MOBA.Game.Client");
    public static readonly Assembly ServerAssembly = Assembly.Load("MOBA.Server");
    public static readonly Assembly ClientAssembly = Assembly.Load("MOBA.Client");

    // `Silk.NET.Windowing` and `Silk.NET.Input` are metapackages; the actual assemblies
    // that hold the windowing/input types are the `.Common` suffixed ones.
    public static readonly Assembly SilkOpenGLAssembly = Assembly.Load("Silk.NET.OpenGL");
    public static readonly Assembly SilkWindowingAssembly = Assembly.Load("Silk.NET.Windowing.Common");
    public static readonly Assembly SilkInputAssembly = Assembly.Load("Silk.NET.Input.Common");

    public static readonly ArchUnitNET.Domain.Architecture Instance = new ArchLoader()
        .LoadAssemblies(
            UtilitiesAssembly,
            EngineCoreAssembly,
            EngineGraphicsAssembly,
            EngineGraphicsOpenGLAssembly,
            EngineNetworkingAssembly,
            GameAssembly,
            GameClientAssembly,
            ServerAssembly,
            ClientAssembly,
            SilkOpenGLAssembly,
            SilkWindowingAssembly,
            SilkInputAssembly)
        .Build();
}
