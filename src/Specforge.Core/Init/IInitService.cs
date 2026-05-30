namespace Specforge.Core.Init;

/// <summary>Orchestrates a full <c>init</c> run: config write + behavioral files + optional scaffold.</summary>
public interface IInitService
{
    Task<InitResult> RunAsync(InitOptions options, CancellationToken ct);
}
