namespace Specforge.Core.Init;

/// <summary>One package entry supplied to <c>init</c> (DEC-002 / DEC-004).</summary>
/// <param name="Name">Package name.</param>
/// <param name="Path">Package directory path, relative to the project root.</param>
/// <param name="ExtraKinds">Optional per-package extra ID kinds (DEC-004).</param>
public sealed record InitPackageInput(string Name, string Path, IReadOnlyList<string>? ExtraKinds = null);
