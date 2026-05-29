using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Specforge.Mcp.Hosting;

/// <summary>
/// DEC-003 §"Logging and Diagnostics": all log output goes to stderr (stdout is the MCP channel).
/// Default level <c>Information</c>, overridable via <c>SPECFORGE_LOG_LEVEL</c>; optionally tee'd to
/// <c>SPECFORGE_LOG_FILE</c>.
/// </summary>
public static class StderrLoggingConfiguration
{
    /// <summary>Environment variable selecting the minimum log level.</summary>
    public const string LogLevelEnvVar = "SPECFORGE_LOG_LEVEL";

    /// <summary>Environment variable selecting an optional log file to tee into.</summary>
    public const string LogFileEnvVar = "SPECFORGE_LOG_FILE";

    /// <summary>Configures logging so every level routes to stderr and never to stdout.</summary>
    public static IServiceCollection AddSpecforgeStderrLogging(this IServiceCollection services)
    {
        LogLevel minLevel = ParseLevel(Environment.GetEnvironmentVariable(LogLevelEnvVar)) ?? LogLevel.Information;
        string? logFile = Environment.GetEnvironmentVariable(LogFileEnvVar);

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(minLevel);
            builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

            if (!string.IsNullOrWhiteSpace(logFile))
            {
                builder.AddProvider(new FileLoggerProvider(logFile));
            }
        });

        return services;
    }

    private static LogLevel? ParseLevel(string? raw) =>
        Enum.TryParse(raw, ignoreCase: true, out LogLevel level) ? level : null;
}
