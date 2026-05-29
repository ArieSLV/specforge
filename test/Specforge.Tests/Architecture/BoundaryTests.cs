using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Specforge.Core;

using Xunit;

namespace Specforge.Tests.Architecture;

/// <summary>
/// Enforces the dependency direction from DEC-003-RUNTIME-AND-ARCHITECTURE §"Dependency Direction":
/// <c>Specforge.Core</c> must not depend on the MCP host stack or ASP.NET / generic Host
/// libraries. The test reads <c>Specforge.Core.dll</c>'s PE metadata directly and inspects
/// the AssemblyRef table — no runtime activation is required.
/// </summary>
public class BoundaryTests
{
    [Fact]
    public void SpecforgeCore_ReferencesNo_McpAssemblies()
    {
        string[] forbiddenPrefixes = ["ModelContextProtocol"];
        string[] violations = GetReferencedAssemblyNames(GetCoreAssemblyPath())
            .Where(name => forbiddenPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Specforge.Core must not reference any ModelContextProtocol.* assembly, but found: [{string.Join(", ", violations)}]. " +
            "Per DEC-003 §'Dependency Direction', MCP wiring lives in Specforge.Mcp only.");
    }

    [Fact]
    public void SpecforgeCore_ReferencesNo_HttpHosting()
    {
        string[] forbiddenPrefixes =
        [
            "Microsoft.AspNetCore.",
            "Microsoft.Extensions.Hosting",
        ];
        string[] violations = GetReferencedAssemblyNames(GetCoreAssemblyPath())
            .Where(name => forbiddenPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Specforge.Core must not reference any HTTP / Hosting assembly, but found: [{string.Join(", ", violations)}]. " +
            "Per DEC-003 §'Dependency Direction', Core is plain-library code only.");
    }

    private static string GetCoreAssemblyPath()
    {
        // Resolve via the public AssemblyMarker from Specforge.Core — that way the
        // ProjectReference in Specforge.Tests.csproj guarantees Specforge.Core.dll
        // is copied next to the test binary at build time.
        return typeof(AssemblyMarker).Assembly.Location;
    }

    private static IEnumerable<string> GetReferencedAssemblyNames(string assemblyPath)
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new(stream);
        MetadataReader metadata = peReader.GetMetadataReader();

        List<string> names = [];
        foreach (AssemblyReferenceHandle handle in metadata.AssemblyReferences)
        {
            AssemblyReference reference = metadata.GetAssemblyReference(handle);
            names.Add(metadata.GetString(reference.Name));
        }

        return names;
    }
}
