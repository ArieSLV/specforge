using System.Text.Json;

using Specforge.Core.Configuration;
using Specforge.Core.Documents;
using Specforge.Core.Identifiers;
using Specforge.Core.Ledger;
using Specforge.Core.Lifecycle;
using Specforge.Mcp.Hosting;
using Specforge.Mcp.Tools;
using Specforge.Mcp.Tools.Items;
using Specforge.Tests.TestSupport;

using Xunit;

namespace Specforge.Tests.Tools.Items;

public class ItemToolsTests
{
    private static ItemService ServiceFor(PackageWorkspace ws) => new(
        ws.Session,
        new IdAllocator(new LedgerReader(), ws.Session),
        new ArtifactLedgerService(ws.Session),
        new HistoryLedgerService(ws.Session),
        new ReviewLedgerService(ws.Session),
        new LifecycleStateMachine(),
        new ItemFileWriter());

    private static SpecforgeWorkingDirectory Cwd(PackageWorkspace ws) => new(ws.Root);

    private static JsonElement Args(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    [Fact]
    public async Task Create_List_Get_RoundTrip()
    {
        using PackageWorkspace ws = new();
        ItemService service = ServiceFor(ws);
        ConfigLoader loader = new();
        SpecforgeWorkingDirectory cwd = Cwd(ws);

        ToolResult created = await new CreateItemTool(loader, ws.Session, cwd, service)
            .InvokeAsync(Args("""{"title":"My Item","dependsOn":["DEC-001"]}"""), CancellationToken.None);
        Assert.Equal("ITEM-001", Assert.IsType<ToolResult.Success>(created).Payload.GetProperty("id").GetString());

        ToolResult listed = await new ListItemsTool(loader, ws.Session, cwd, service).InvokeAsync(Args("{}"), CancellationToken.None);
        Assert.Single(Assert.IsType<ToolResult.Success>(listed).Payload.GetProperty("items").EnumerateArray());

        ToolResult got = await new GetItemTool(loader, ws.Session, cwd, service).InvokeAsync(Args("""{"id":"ITEM-001"}"""), CancellationToken.None);
        JsonElement payload = Assert.IsType<ToolResult.Success>(got).Payload;
        Assert.Equal("My Item", payload.GetProperty("title").GetString());
        Assert.Empty(payload.GetProperty("missingRequiredSections").EnumerateArray());
    }

    [Fact]
    public async Task Get_MissingId_ReturnsInvalidArgument()
    {
        using PackageWorkspace ws = new();
        ToolResult result = await new GetItemTool(new ConfigLoader(), ws.Session, Cwd(ws), ServiceFor(ws))
            .InvokeAsync(Args("{}"), CancellationToken.None);
        Assert.Equal("specforge.tool.invalid_argument", Assert.IsType<ToolResult.Failure>(result).Envelope.Code);
    }

    [Fact]
    public async Task SetStatus_Then_Delete()
    {
        using PackageWorkspace ws = new();
        ItemService service = ServiceFor(ws);
        ConfigLoader loader = new();
        SpecforgeWorkingDirectory cwd = Cwd(ws);

        await new CreateItemTool(loader, ws.Session, cwd, service).InvokeAsync(Args("""{"title":"Topic"}"""), CancellationToken.None);
        ToolResult set = await new SetItemStatusTool(loader, ws.Session, cwd, service)
            .InvokeAsync(Args("""{"id":"ITEM-001","status":"Draft for user review"}"""), CancellationToken.None);
        Assert.Equal("Draft for user review", Assert.IsType<ToolResult.Success>(set).Payload.GetProperty("newStatus").GetString());

        ToolResult deleted = await new DeleteItemTool(loader, ws.Session, cwd, service)
            .InvokeAsync(Args("""{"id":"ITEM-001","confirm":true}"""), CancellationToken.None);
        Assert.IsType<ToolResult.Success>(deleted);
    }
}
