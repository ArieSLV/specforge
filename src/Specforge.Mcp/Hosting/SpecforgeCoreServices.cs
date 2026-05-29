using Microsoft.Extensions.DependencyInjection;

using Specforge.Core.Configuration;
using Specforge.Core.Diagnostics;

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
        return services;
    }
}
