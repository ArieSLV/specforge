using Microsoft.Extensions.DependencyInjection;

using Specforge.Core.Configuration;
using Specforge.Core.Diagnostics;
using Specforge.Core.Documents;
using Specforge.Core.Identifiers;
using Specforge.Core.Init;
using Specforge.Core.Ledger;
using Specforge.Core.Lifecycle;
using Specforge.Core.Skills;
using Specforge.Core.Validation;

namespace Specforge.Mcp.Hosting;

/// <summary>
/// Registers the reusable <c>Specforge.Core</c> services as singletons (DEC-003 dependency direction).
/// Every later tool-bearing item reuses this without re-architecting.
/// </summary>
public static class SpecforgeCoreServices
{
    /// <summary>Adds <see cref="ConfigLoader"/>, <see cref="SessionState"/> and <see cref="BinaryInfo"/>.</summary>
    public static IServiceCollection AddSpecforgeCore(this IServiceCollection services)
    {
        services.AddSingleton<ConfigLoader>();
        services.AddSingleton<SessionState>();
        services.AddSingleton<BinaryInfo>();

        // ITEM-003 identifier services. KindRegistry is rebuilt per resolution from the live session
        // (a new use_package selection swaps in a new registry).
        services.AddSingleton<ILedgerReader, LedgerReader>();
        services.AddSingleton<IIdAllocator, IdAllocator>();
        services.AddTransient(sp => KindRegistry.FromSession(sp.GetRequiredService<SessionState>()));

        // ITEM-004 embedded skill catalog (DEC-005): enumerated from the Core assembly's manifest resources.
        services.AddSingleton<IEmbeddedSkillCatalog>(_ => new EmbeddedSkillCatalog(typeof(SpecforgeConfig).Assembly));
        services.AddSingleton<SkillCatalogValidator>();

        // ITEM-005 skill installer (DEC-005): writes the catalog to user-wide agent directories.
        services.AddSingleton<ISkillInstallTargetResolver, SkillInstallTargetResolver>();
        services.AddSingleton<ISkillInstaller, SkillInstaller>();

        // ITEM-006 init (DEC-002/005/006/007/008): config write + behavioral files + scaffold.
        services.AddSingleton<IEmbeddedTemplateCatalog>(_ => new EmbeddedTemplateCatalog(typeof(SpecforgeConfig).Assembly));
        services.AddSingleton<ConfigWriter>();
        services.AddSingleton<SpecforgeMdGenerator>();
        services.AddSingleton<TaggedBlockMerger>();
        services.AddSingleton<CodexOpenaiYamlWriter>();
        services.AddSingleton<ScaffoldEngine>();
        services.AddSingleton<IInitService, InitService>();

        // ITEM-007 ledger primitives + lifecycle + decision service (first spec-graph tools).
        services.AddSingleton<IArtifactLedgerService, ArtifactLedgerService>();
        services.AddSingleton<IHistoryLedgerService, HistoryLedgerService>();
        services.AddSingleton<IReviewLedgerService, ReviewLedgerService>();
        services.AddSingleton<ICommitLedgerService, CommitLedgerService>();
        services.AddSingleton<LifecycleStateMachine>();
        services.AddSingleton<DecisionFileWriter>();
        services.AddSingleton<DecisionService>();

        // ITEM-008 item-side document layer (reuses the ledger/lifecycle infra above).
        services.AddSingleton<ItemFileWriter>();
        services.AddSingleton<ItemService>();

        // ITEM-010 validation layer: tombstone-aware allocator decorator + 4 aspect validators + orchestrator.
        services.AddSingleton(sp => new TombstoneAwareIdAllocator(sp.GetRequiredService<IIdAllocator>(), sp.GetRequiredService<SessionState>()));
        services.AddSingleton<IIdsAspectValidator, IdsAspectValidator>();
        services.AddSingleton<ILinksAspectValidator, LinksAspectValidator>();
        services.AddSingleton<ILifecycleAspectValidator, LifecycleAspectValidator>();
        services.AddSingleton<IImpactCoverageAspectValidator, ImpactCoverageAspectValidator>();
        services.AddSingleton<IValidationService, ValidationService>();
        return services;
    }
}
