using System.Text.Json;

using Specforge.Core.Exceptions;
using Specforge.Core.Init;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;

using Xunit;

namespace Specforge.Tests.Tools;

public class InitToolTests
{
    private sealed class FakeInitService(Func<InitOptions, InitResult> impl) : IInitService
    {
        public InitOptions? LastOptions { get; private set; }

        public Task<InitResult> RunAsync(InitOptions options, CancellationToken ct)
        {
            LastOptions = options;
            return Task.FromResult(impl(options));
        }
    }

    private sealed class ThrowingInitService(Exception exception) : IInitService
    {
        public Task<InitResult> RunAsync(InitOptions options, CancellationToken ct) => throw exception;
    }

    private static InitResult SampleResult() =>
        new(new InitSectionResult(["/root/.specforge.json"], [], []), InitSectionResult.Empty, InitSectionResult.Empty, false);

    private static InitTool ToolWith(IInitService service) => new(service, new SpecforgeWorkingDirectory("/root"));

    private static JsonElement Args(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    [Fact]
    public async Task Invoke_Success_ReturnsPayload()
    {
        InitTool tool = ToolWith(new FakeInitService(_ => SampleResult()));

        ToolResult result = await tool.InvokeAsync(Args("""{"packages":[{"name":"p","path":"spec/p"}]}"""), CancellationToken.None);

        JsonElement payload = Assert.IsType<ToolResult.Success>(result).Payload;
        Assert.Equal(1, payload.GetProperty("config").GetProperty("writtenCount").GetInt32());
        Assert.False(payload.GetProperty("dryRun").GetBoolean());
    }

    [Fact]
    public async Task Invoke_MissingPackages_ReturnsInvalidArgument()
    {
        InitTool tool = ToolWith(new FakeInitService(_ => SampleResult()));

        ToolResult result = await tool.InvokeAsync(Args("{}"), CancellationToken.None);

        ToolResult.Failure failure = Assert.IsType<ToolResult.Failure>(result);
        Assert.Equal("specforge.tool.invalid_argument", failure.Envelope.Code);
    }

    [Fact]
    public async Task Invoke_BadBehavioralFiles_ReturnsInvalidArgument()
    {
        InitTool tool = ToolWith(new FakeInitService(_ => SampleResult()));

        ToolResult result = await tool.InvokeAsync(
            Args("""{"packages":[{"name":"p","path":"spec/p"}],"behavioralFiles":"bogus"}"""),
            CancellationToken.None);

        Assert.Equal("specforge.tool.invalid_argument", Assert.IsType<ToolResult.Failure>(result).Envelope.Code);
    }

    [Fact]
    public async Task Invoke_PassesOptionsThrough()
    {
        FakeInitService service = new(_ => SampleResult());
        InitTool tool = ToolWith(service);

        await tool.InvokeAsync(
            Args("""{"packages":[{"name":"p","path":"spec/p"}],"scaffold":true,"behavioralFiles":"none"}"""),
            CancellationToken.None);

        Assert.NotNull(service.LastOptions);
        Assert.True(service.LastOptions!.Scaffold);
        Assert.Equal(BehavioralFilesScope.None, service.LastOptions.BehavioralFiles);
        Assert.Equal("/root", service.LastOptions.Root);
        Assert.Single(service.LastOptions.Packages);
    }

    [Fact]
    public async Task Invoke_ServiceThrows_Propagates()
    {
        InitTool tool = ToolWith(new ThrowingInitService(
            new SpecforgeInvalidArgumentException("CLAUDE.md", "balanced block", "remove the partial block")));

        await Assert.ThrowsAsync<SpecforgeInvalidArgumentException>(
            () => tool.InvokeAsync(Args("""{"packages":[{"name":"p","path":"spec/p"}]}"""), CancellationToken.None));
    }
}
