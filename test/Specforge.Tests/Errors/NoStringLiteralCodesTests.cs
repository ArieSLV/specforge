using System.Text.RegularExpressions;

using Xunit;

namespace Specforge.Tests.Errors;

/// <summary>
/// Source-grep invariant (ITEM-011): no <c>Specforge.Mcp</c> source file contains a hard-coded
/// <c>"specforge.&lt;domain&gt;.&lt;reason&gt;"</c> string literal. Every code reference goes through the
/// <c>SpecforgeErrorCode</c> constant store (which lives in <c>Specforge.Core</c>).
/// </summary>
public class NoStringLiteralCodesTests
{
    private static string? FindMcpSource()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "src", "Specforge.Mcp");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }

    [Fact]
    public void McpSource_HasNoHardCodedErrorCodeLiterals()
    {
        string? root = FindMcpSource();
        Assert.True(root is not null, "src/Specforge.Mcp not found by walk-up from the test base directory");

        Regex codeLiteral = new("\"specforge\\.[a-z_]+\\.[a-z_]+");
        List<string> offenders = [];
        foreach (string file in Directory.EnumerateFiles(root!, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (codeLiteral.IsMatch(lines[i]))
                {
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0, $"hard-coded error-code literals found in Specforge.Mcp:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }
}
