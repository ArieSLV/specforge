using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;
using Specforge.Core.Identifiers;

using Xunit;

namespace Specforge.Tests.Identifiers;

public class IdAllocatorTests
{
    private sealed class FakeLedgerReader(IReadOnlyDictionary<string, IReadOnlyList<string>> byFileName) : ILedgerReader
    {
        public Task<IReadOnlyList<string>> ReadLedgerIdsAsync(string ledgerFilePath, CancellationToken ct)
        {
            string name = Path.GetFileName(ledgerFilePath);
            return Task.FromResult(byFileName.TryGetValue(name, out IReadOnlyList<string>? ids) ? ids : []);
        }
    }

    private static SessionState SessionWithPackage()
    {
        SpecforgePackageConfig package = new("pkg", ".", SpecforgePackageConfig.NoExtraKinds);
        SpecforgeConfig config = new(1, "spec/shared", "spec/templates", [package], "/repo/.specforge.json");
        SessionState session = new();
        session.Initialize(config);
        return session;
    }

    private static IdAllocator Allocator(params (string FileName, string[] Ids)[] files)
    {
        Dictionary<string, IReadOnlyList<string>> map = files.ToDictionary(f => f.FileName, f => (IReadOnlyList<string>)f.Ids);
        return new IdAllocator(new FakeLedgerReader(map), SessionWithPackage());
    }

    [Fact]
    public async Task NextNumber_Decision_ReturnsHighestPlusOne()
    {
        IdAllocator allocator = Allocator(("artifacts.md", ["ART-DEC-001", "ART-DEC-002", "ART-DEC-003"]));
        Assert.Equal(4, await allocator.NextNumberAsync("DEC", "pkg", CancellationToken.None));
    }

    [Fact]
    public async Task NextNumber_Item_ReturnsHighestPlusOne()
    {
        IdAllocator allocator = Allocator(("artifacts.md", ["ART-ITEM-001"]));
        Assert.Equal(2, await allocator.NextNumberAsync("ITEM", "pkg", CancellationToken.None));
    }

    [Fact]
    public async Task NextNumber_EmptyLedger_ReturnsOne()
    {
        IdAllocator allocator = Allocator();
        Assert.Equal(1, await allocator.NextNumberAsync("DEC", "pkg", CancellationToken.None));
    }

    [Fact]
    public async Task NextNumber_Commit_ScansCommitsLedger()
    {
        IdAllocator allocator = Allocator(("commits.md", ["CMT-001", "CMT-002"]));
        Assert.Equal(3, await allocator.NextNumberAsync("CMT", "pkg", CancellationToken.None));
    }

    [Fact]
    public async Task NextNumber_IgnoresDescriptiveArtRows()
    {
        IdAllocator allocator = Allocator(("artifacts.md", ["ART-WORK-PLAN", "ART-DEC-005", "ART-GLOSSARY"]));
        Assert.Equal(6, await allocator.NextNumberAsync("DEC", "pkg", CancellationToken.None));
    }

    [Fact]
    public async Task NextNumber_KindExhausted_Throws()
    {
        IdAllocator allocator = Allocator(("artifacts.md", ["ART-XYZ-999"]));
        SpecforgeKindExhaustedException ex = await Assert.ThrowsAsync<SpecforgeKindExhaustedException>(
            () => allocator.NextNumberAsync("XYZ", "pkg", CancellationToken.None));
        Assert.Equal("XYZ", ex.Kind);
        Assert.Equal("pkg", ex.Package);
    }
}
