using Specforge.Core.Configuration;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;
using Specforge.Core.Lifecycle;
using Specforge.Core.Validation;

using Xunit;

namespace Specforge.Tests.Validation;

/// <summary>
/// The marquee self-consistency proof (ITEM-010): running <c>validate(aspect=all)</c> against the very
/// spec graph used to specify specforge must come back with zero errors.
/// </summary>
public class DogfoodValidationTests
{
    private static string? FindRepoConfig()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, ".specforge.json")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static ValidationService ServiceFor(SessionState session)
    {
        TombstoneAwareIdAllocator allocator = new(new IdAllocator(new LedgerReader(), session), session);
        return new ValidationService(
            new IdsAspectValidator(session, allocator),
            new LinksAspectValidator(session, allocator),
            new LifecycleAspectValidator(session, new ArtifactLedgerService(session), new LifecycleStateMachine()),
            new ImpactCoverageAspectValidator(session));
    }

    [Fact]
    public async Task ValidateAll_LiveSpecGraph_HasNoErrors()
    {
        string? repoRoot = FindRepoConfig();
        Assert.True(repoRoot is not null, ".specforge.json not found by walk-up from the test base directory");

        SpecforgeConfig config = await new ConfigLoader().LoadAsync(repoRoot!, CancellationToken.None);
        SessionState session = new();
        session.Initialize(config);

        ValidationResult result = await ServiceFor(session).ValidateAsync(ValidationAspect.All, CancellationToken.None);

        string dump = string.Join(
            Environment.NewLine,
            result.Findings.Select(f => $"  [{f.Severity}] {f.Aspect} {f.Target}: {f.Message}"));
        Assert.True(result.ErrorCount == 0, $"Expected 0 errors in the live spec graph but found {result.ErrorCount}:{Environment.NewLine}{dump}");
    }
}
