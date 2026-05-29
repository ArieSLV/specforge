using Microsoft.Extensions.DependencyInjection;

using Specforge.Core.Configuration;
using Specforge.Core.Diagnostics;
using Specforge.Core.Identifiers;
using Specforge.Core.Skills;

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
        return services;
    }
}
