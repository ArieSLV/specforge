namespace Specforge.Core.Init;

/// <summary>Inputs to an <c>init</c> run (DEC-002/005/006/007/008).</summary>
/// <param name="Root">Absolute project root the init writes into.</param>
/// <param name="Packages">Packages to write into <c>.specforge.json</c> (full replacement on re-init).</param>
/// <param name="Shared">Repo-level shared directory, relative to the root.</param>
/// <param name="Templates">Repo-level templates directory, relative to the root.</param>
/// <param name="Scaffold">When true, also create package skeletons and copy embedded shared/templates.</param>
/// <param name="BehavioralFiles">Which behavioral files to write (DEC-008).</param>
/// <param name="DryRun">When true, plan writes without touching the filesystem.</param>
public sealed record InitOptions(
    string Root,
    IReadOnlyList<InitPackageInput> Packages,
    string Shared = "spec/shared",
    string Templates = "spec/templates",
    bool Scaffold = false,
    BehavioralFilesScope BehavioralFiles = BehavioralFilesScope.All,
    bool DryRun = false);
