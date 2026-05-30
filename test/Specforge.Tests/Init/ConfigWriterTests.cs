using Specforge.Core.Configuration;
using Specforge.Core.Init;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Init;

public class ConfigWriterTests
{
    private static readonly ConfigWriter Writer = new();

    private static SpecforgeConfig Config(IReadOnlyList<string>? extraKinds = null) => new(
        1, "spec/shared", "spec/templates",
        [new SpecforgePackageConfig("specforge-mvp", "spec/packages/specforge-mvp", extraKinds ?? SpecforgePackageConfig.NoExtraKinds)],
        "/repo/.specforge.json");

    [Fact]
    public void Serialize_ProducesCanonicalJson()
    {
        string json = Writer.Serialize(Config());

        Assert.Contains("\"schemaVersion\": 1", json, StringComparison.Ordinal);
        Assert.Contains("\"shared\": \"spec/shared\"", json, StringComparison.Ordinal);
        Assert.Contains("\"name\": \"specforge-mvp\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("extraKinds", json, StringComparison.Ordinal);
        Assert.EndsWith("\n", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_IncludesExtraKindsWhenPresent()
    {
        string json = Writer.Serialize(Config(["RDM", "BSP"]));
        Assert.Contains("extraKinds", json, StringComparison.Ordinal);
        Assert.Contains("RDM", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_IsDeterministic() =>
        Assert.Equal(Writer.Serialize(Config()), Writer.Serialize(Config()));

    [Fact]
    public async Task WriteAtomic_WritesThenOverwrites()
    {
        using TempWorkspace ws = new();
        string path = Path.Combine(ws.Root, ".specforge.json");

        await Writer.WriteAtomicAsync("first", path, CancellationToken.None);
        Assert.Equal("first", await File.ReadAllTextAsync(path));

        await Writer.WriteAtomicAsync("second", path, CancellationToken.None);
        Assert.Equal("second", await File.ReadAllTextAsync(path));
    }
}
