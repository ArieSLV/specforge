using Specforge.Core.Documents;

using Xunit;

namespace Specforge.Tests.Documents;

public class RequiredItemSectionsContractTests
{
    private static string? FindContract()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "spec", "shared", "spec_item_contract.md");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }

    [Fact]
    public void EveryRequiredSection_AppearsInTheSharedContract()
    {
        string? path = FindContract();
        Assert.True(path is not null, "spec/shared/spec_item_contract.md not found by walk-up from the test base directory");

        string contract = File.ReadAllText(path!);
        foreach (string section in RequiredItemSections.Names)
        {
            Assert.Contains($"## {section}", contract, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RequiredSections_AreASubsetOfTheFullTemplate()
    {
        foreach (string section in RequiredItemSections.Names)
        {
            Assert.Contains(section, RequiredItemSections.FullTemplate);
        }
    }
}
