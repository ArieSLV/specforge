using Specforge.Core.Init;

using Xunit;

namespace Specforge.Tests.Init;

public class CodexOpenaiYamlWriterTests
{
    [Fact]
    public void Generate_DeclaresSpecforgeMcpServer()
    {
        string yaml = new CodexOpenaiYamlWriter().Generate();
        Assert.Contains("mcp_servers:", yaml, StringComparison.Ordinal);
        Assert.Contains("specforge:", yaml, StringComparison.Ordinal);
        Assert.Contains("command: specforge", yaml, StringComparison.Ordinal);
    }
}
