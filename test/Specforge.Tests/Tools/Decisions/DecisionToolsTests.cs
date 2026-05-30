using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Core.Exceptions;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;
using Specforge.Core.Lifecycle;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;
using Specforge.Mcp.Tools.Decisions;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Tools.Decisions;

public class DecisionToolsTests
{
    private static DecisionService ServiceFor(PackageWorkspace ws) => new(
        ws.Session,
        new IdAllocator(new LedgerReader(), ws.Session),
        new ArtifactLedgerService(ws.Session),
        new HistoryLedgerService(ws.Session),
        new ReviewLedgerService(ws.Session),
        new LifecycleStateMachine(),
        new DecisionFileWriter());

    private static SpecforgeWorkingDirectory Cwd(PackageWorkspace ws) => new(ws.Root);

    private static JsonElement Args(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    [Fact]
    public async Task Create_List_Get_RoundTrip()
    {
        using PackageWorkspace ws = new();
        DecisionService service = ServiceFor(ws);
        ConfigLoader loader = new();
        SpecforgeWorkingDirectory cwd = Cwd(ws);

        ToolResult created = await new CreateDecisionTool(loader, ws.Session, cwd, service)
            .InvokeAsync(Args("""{"title":"My Topic"}"""), CancellationToken.None);
        Assert.Equal("DEC-001", Assert.IsType<ToolResult.Success>(created).Payload.GetProperty("id").GetString());

        ToolResult listed = await new ListDecisionsTool(loader, ws.Session, cwd, service)
            .InvokeAsync(Args("{}"), CancellationToken.None);
        Assert.Single(Assert.IsType<ToolResult.Success>(listed).Payload.GetProperty("decisions").EnumerateArray());

        ToolResult got = await new GetDecisionTool(loader, ws.Session, cwd, service)
            .InvokeAsync(Args("""{"id":"DEC-001"}"""), CancellationToken.None);
        Assert.Equal("My Topic", Assert.IsType<ToolResult.Success>(got).Payload.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Get_MissingId_ReturnsInvalidArgument()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await new GetDecisionTool(new ConfigLoader(), ws.Session, Cwd(ws), ServiceFor(ws))
            .InvokeAsync(Args("{}"), CancellationToken.None);
        Assert.Equal("specforge.tool.invalid_argument", Assert.IsType<ToolResult.Failure>(result).Envelope.Code);
    }

    [Fact]
    public async Task SetStatus_Then_Delete()
    {
        using PackageWorkspace ws = new();
        DecisionService service = ServiceFor(ws);
        ConfigLoader loader = new();
        SpecforgeWorkingDirectory cwd = Cwd(ws);

        await new CreateDecisionTool(loader, ws.Session, cwd, service).InvokeAsync(Args("""{"title":"Topic"}"""), CancellationToken.None);

        ToolResult set = await new SetDecisionStatusTool(loader, ws.Session, cwd, service)
            .InvokeAsync(Args("""{"id":"DEC-001","status":"Draft for user review"}"""), CancellationToken.None);
        Assert.Equal("Draft for user review", Assert.IsType<ToolResult.Success>(set).Payload.GetProperty("newStatus").GetString());

        ToolResult deleted = await new DeleteDecisionTool(loader, ws.Session, cwd, service)
            .InvokeAsync(Args("""{"id":"DEC-001","confirm":true}"""), CancellationToken.None);
        Assert.IsType<ToolResult.Success>(deleted);
    }

    [Fact]
    public async Task Delete_NoConfirm_Propagates()
    {
        using PackageWorkspace ws = new();
        DecisionService service = ServiceFor(ws);
        ConfigLoader loader = new();
        SpecforgeWorkingDirectory cwd = Cwd(ws);
        await new CreateDecisionTool(loader, ws.Session, cwd, service).InvokeAsync(Args("""{"title":"Topic"}"""), CancellationToken.None);

        await Assert.ThrowsAsync<SpecforgeInvalidArgumentException>(
            () => new DeleteDecisionTool(loader, ws.Session, cwd, service).InvokeAsync(Args("""{"id":"DEC-001"}"""), CancellationToken.None));
    }
}
