using System.Globalization;

using Specforge.Core.Configuration;
using Specforge.Core.Diagnostics;

namespace Specforge.Core.Init;

/// <summary>
/// High-level <c>init</c> orchestration (DEC-002/005/006/007/008): write <c>.specforge.json</c>,
/// the behavioral files (SPECFORGE.md + tagged blocks per <c>behavioralFiles</c>), the always-written
/// Codex <c>agents/openai.yaml</c>, and (when requested) the scaffold tree.
/// </summary>
public sealed class InitService(
    ConfigWriter configWriter,
    SpecforgeMdGenerator specforgeMdGenerator,
    TaggedBlockMerger taggedBlockMerger,
    CodexOpenaiYamlWriter codexYamlWriter,
    ScaffoldEngine scaffoldEngine) : IInitService
{
    // DEC-008 §"Tagged Block in CLAUDE.md and AGENTS.md" — reproduced verbatim (stable public contract).
    private const string TaggedBlockBody =
        "This project uses specforge for spec-driven development.\n\n" +
        "See `./SPECFORGE.md` for the project's spec layout, identifier scheme, available tools and skills, and lifecycle.\n\n" +
        "When the user asks to draft, review, validate, or onboard a spec artifact, prefer the matching specforge skill " +
        "(`draft-decision`, `review-decision`, `draft-item`, `impact-assessment`, `validate-spec-graph`, `adopt-existing-project`) " +
        "and the underlying MCP tools over freeform file edits.\n\n" +
        "For multi-package projects, call `use_package` at the start of the session before any package-dependent tool.";

    public async Task<InitResult> RunAsync(InitOptions options, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(options);

        (SpecforgeConfig config, InitSectionResult configSection) = await WriteConfigAsync(options, ct).ConfigureAwait(false);
        InitSectionResult behavioral = await WriteBehavioralAsync(options, config, ct).ConfigureAwait(false);
        InitSectionResult scaffold = options.Scaffold
            ? await scaffoldEngine.RunAsync(options, ct).ConfigureAwait(false)
            : InitSectionResult.Empty;

        return new InitResult(configSection, behavioral, scaffold, options.DryRun);
    }

    private async Task<(SpecforgeConfig Config, InitSectionResult Section)> WriteConfigAsync(InitOptions options, CancellationToken ct)
    {
        string configPath = Path.Combine(options.Root, ConfigDiscovery.ConfigFileName);
        bool existed = File.Exists(configPath);
        if (existed)
        {
            // Validate + apply the (v1-identity) upgrade chain. A future-version or malformed existing
            // file surfaces config.schema_version_unsupported / config.validation_failed rather than being clobbered.
            string existingText = await File.ReadAllTextAsync(configPath, ct).ConfigureAwait(false);
            _ = ConfigLoader.ParseAndValidate(existingText, configPath);
        }

        List<SpecforgePackageConfig> packages = [.. options.Packages.Select(p =>
            new SpecforgePackageConfig(p.Name, p.Path, p.ExtraKinds ?? SpecforgePackageConfig.NoExtraKinds))];
        SpecforgeConfig config = new(BinaryInfo.SupportedSchemaVersionMax, options.Shared, options.Templates, packages, configPath);
        ConfigValidator.EnforceReservedKinds(config.Packages);

        string content = configWriter.Serialize(config);
        if (!options.DryRun)
        {
            await configWriter.WriteAtomicAsync(content, configPath, ct).ConfigureAwait(false);
        }

        return (config, new InitSectionResult([configPath], existed ? [configPath] : [], []));
    }

    private async Task<InitSectionResult> WriteBehavioralAsync(InitOptions options, SpecforgeConfig config, CancellationToken ct)
    {
        List<string> written = [];
        List<string> overwritten = [];
        List<string> skipped = [];

        // agents/openai.yaml — always written (DEC-005 boundary), independent of behavioralFiles.
        await WriteOwnedAsync(Path.Combine(options.Root, "agents", "openai.yaml"), codexYamlWriter.Generate(), options.DryRun, written, overwritten, ct).ConfigureAwait(false);

        BehavioralFilesScope scope = options.BehavioralFiles;
        string specforgeMdPath = Path.Combine(options.Root, "SPECFORGE.md");
        string claudePath = Path.Combine(options.Root, "CLAUDE.md");
        string agentsPath = Path.Combine(options.Root, "AGENTS.md");

        if (scope != BehavioralFilesScope.None)
        {
            string projectName = new DirectoryInfo(options.Root).Name;
            string generatedOn = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string markdown = specforgeMdGenerator.Generate(config, projectName, generatedOn);
            await WriteOwnedAsync(specforgeMdPath, markdown, options.DryRun, written, overwritten, ct).ConfigureAwait(false);
        }
        else
        {
            skipped.Add(specforgeMdPath);
        }

        if (scope is BehavioralFilesScope.All or BehavioralFilesScope.ClaudeOnly)
        {
            await MergeBlockAsync(claudePath, options.DryRun, written, overwritten, ct).ConfigureAwait(false);
        }
        else
        {
            skipped.Add(claudePath);
        }

        if (scope is BehavioralFilesScope.All or BehavioralFilesScope.CodexOnly)
        {
            await MergeBlockAsync(agentsPath, options.DryRun, written, overwritten, ct).ConfigureAwait(false);
        }
        else
        {
            skipped.Add(agentsPath);
        }

        return new InitSectionResult(written, overwritten, skipped);
    }

    private static async Task WriteOwnedAsync(string path, string content, bool dryRun, List<string> written, List<string> overwritten, CancellationToken ct)
    {
        bool existed = File.Exists(path);
        written.Add(path);
        if (existed)
        {
            overwritten.Add(path);
        }

        if (!dryRun)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, content, ct).ConfigureAwait(false);
        }
    }

    private async Task MergeBlockAsync(string path, bool dryRun, List<string> written, List<string> overwritten, CancellationToken ct)
    {
        string? existing = File.Exists(path) ? await File.ReadAllTextAsync(path, ct).ConfigureAwait(false) : null;
        string merged = taggedBlockMerger.Merge(existing, TaggedBlockBody, path); // throws invalid_argument on an imbalanced block
        written.Add(path);
        if (existing is not null)
        {
            overwritten.Add(path);
        }

        if (!dryRun)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, merged, ct).ConfigureAwait(false);
        }
    }
}
