using System.Text.Json;

using Specforge.Core.Exceptions;
using Specforge.Core.Skills;
using Specforge.Mcp.Tools;

using Xunit;

namespace Specforge.Tests.Tools;

public class InstallSkillsToolTests
{
    private sealed class FakeInstaller(Func<SkillInstallAgent, bool, SkillInstallResult> impl) : ISkillInstaller
    {
        public SkillInstallAgent? LastAgent { get; private set; }

        public bool? LastDryRun { get; private set; }

        public Task<SkillInstallResult> InstallAsync(SkillInstallAgent agent, bool dryRun, CancellationToken ct)
        {
            LastAgent = agent;
            LastDryRun = dryRun;
            return Task.FromResult(impl(agent, dryRun));
        }
    }

    private sealed class ThrowingInstaller(SpecforgeSkillInstallException exception) : ISkillInstaller
    {
        public Task<SkillInstallResult> InstallAsync(SkillInstallAgent agent, bool dryRun, CancellationToken ct) => throw exception;
    }

    private static SkillInstallResult ResultWith(int written) => new(
        new Dictionary<string, SkillInstallAgentResult>
        {
            ["claude-code"] = new(written, [.. Enumerable.Range(0, written).Select(i => $"/p/{i}")], []),
        },
        "1.0.0");

    private static JsonElement Args(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    [Fact]
    public async Task Success_EnvelopeShape()
    {
        InstallSkillsTool tool = new(new FakeInstaller((_, _) => ResultWith(2)));

        ToolResult result = await tool.InvokeAsync(Args("""{"agent":"all"}"""), CancellationToken.None);

        JsonElement payload = Assert.IsType<ToolResult.Success>(result).Payload;
        Assert.Equal(2, payload.GetProperty("agents").GetProperty("claude-code").GetProperty("writtenCount").GetInt32());
        Assert.False(string.IsNullOrEmpty(payload.GetProperty("binaryVersion").GetString()));
    }

    [Fact]
    public async Task DryRun_PassedThrough()
    {
        FakeInstaller installer = new((_, _) => ResultWith(0));
        InstallSkillsTool tool = new(installer);

        await tool.InvokeAsync(Args("""{"agent":"claude-code","dryRun":true}"""), CancellationToken.None);

        Assert.Equal(SkillInstallAgent.ClaudeCode, installer.LastAgent);
        Assert.True(installer.LastDryRun);
    }

    [Fact]
    public async Task InvalidAgent_ReturnsInvalidArgument()
    {
        InstallSkillsTool tool = new(new FakeInstaller((_, _) => ResultWith(0)));

        ToolResult result = await tool.InvokeAsync(Args("""{"agent":"bogus"}"""), CancellationToken.None);

        ToolResult.Failure failure = Assert.IsType<ToolResult.Failure>(result);
        Assert.Equal("specforge.tool.invalid_argument", failure.Envelope.Code);
    }

    [Fact]
    public async Task WriteFailure_Propagates()
    {
        SkillInstallResult partial = new(new Dictionary<string, SkillInstallAgentResult>(), "1.0.0");
        InstallSkillsTool tool = new(new ThrowingInstaller(
            new SpecforgeSkillInstallException("codex", "/p/x", typeof(IOException), "denied", partial)));

        await Assert.ThrowsAsync<SpecforgeSkillInstallException>(
            () => tool.InvokeAsync(Args("{}"), CancellationToken.None));
    }
}
