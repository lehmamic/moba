namespace MOBA.Engine.Core.Input;

/// <summary>
/// Engine-portable representation of "which keys are held right now." A bit
/// per key keeps the snapshot allocation-free and turns a held-key check into
/// a single mask AND. Input adapters (Silk.NET-, Avalonia- or future-backed)
/// map their platform keys onto these bits; engine consumers (cameras, input
/// components) only ever see this enum.
/// </summary>
[Flags]
public enum EngineKeys : uint
{
    None = 0,
    W = 1u << 0,
    A = 1u << 1,
    S = 1u << 2,
    D = 1u << 3,
    Q = 1u << 4,
    E = 1u << 5,
}
