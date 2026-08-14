namespace Refactor.Nexus.Api.Tests;

public sealed class CompositionLayoutTests
{
    [Fact]
    public void Operations_composition_does_not_import_mandates_infrastructure()
    {
        var path = FindSource("OperationsServiceCollectionExtensions.cs");
        var source = File.ReadAllText(path);

        Assert.DoesNotContain(
            "Mandates.Infrastructure",
            source,
            StringComparison.Ordinal);
        Assert.Contains("AddRefactorOperations", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using Refactor.Nexus.Api.Mandates.Infrastructure", source, StringComparison.Ordinal);
    }

    private static string FindSource(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "Nexus.Api",
                "Operations",
                "Composition",
                fileName);
            if (File.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new FileNotFoundException(fileName);
    }
}
