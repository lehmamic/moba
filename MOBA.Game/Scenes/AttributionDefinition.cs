namespace MOBA.Game.Scenes;

/// <summary>
/// Asset attribution metadata for third-party-sourced map content. Not used
/// at runtime; documents provenance + licence terms for the map's asset chain.
/// </summary>
public sealed record AttributionDefinition(string Source, string URL, string License, string LicenseURL);
