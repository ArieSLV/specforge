namespace Specforge.Tests.TestSupport;

/// <summary>Canonical <c>.specforge.json</c> fragments for tests.</summary>
public static class ConfigJson
{
    public static string Single(string name = "specforge-mvp", string path = "spec/packages/specforge-mvp") =>
        $$"""
        {
          "schemaVersion": 1,
          "shared": "spec/shared",
          "templates": "spec/templates",
          "packages": [ { "name": "{{name}}", "path": "{{path}}" } ]
        }
        """;

    public static string TwoPackages() =>
        """
        {
          "schemaVersion": 1,
          "shared": "spec/shared",
          "templates": "spec/templates",
          "packages": [
            { "name": "alpha", "path": "spec/packages/alpha" },
            { "name": "beta", "path": "spec/packages/beta" }
          ]
        }
        """;
}
