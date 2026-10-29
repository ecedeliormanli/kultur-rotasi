using System.Reflection;

namespace KulturRotasi.UnitTests.Architecture;

/// <summary>
/// Guards the Clean Architecture dependency rule: inner layers must not reference outer layers.
/// </summary>
public class LayerDependencyTests
{
    private const string Domain = "KulturRotasi.Domain";
    private const string Application = "KulturRotasi.Application";
    private const string Infrastructure = "KulturRotasi.Infrastructure";
    private const string Api = "KulturRotasi.Api";

    [Fact]
    public void Domain_does_not_depend_on_other_layers()
    {
        var references = ReferencedProjects(KulturRotasi.Domain.AssemblyReference.Assembly);

        Assert.Empty(references);
    }

    [Fact]
    public void Domain_does_not_depend_on_entity_framework()
    {
        // The domain model must stay persistence-agnostic: no ORM attributes, no DbContext
        var references = KulturRotasi.Domain.AssemblyReference.Assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));

        Assert.Empty(references);
    }

    [Fact]
    public void Application_depends_only_on_domain()
    {
        var references = ReferencedProjects(KulturRotasi.Application.DependencyInjection.Assembly);

        Assert.DoesNotContain(Infrastructure, references);
        Assert.DoesNotContain(Api, references);
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_api()
    {
        var references = ReferencedProjects(KulturRotasi.Infrastructure.DependencyInjection.Assembly);

        Assert.DoesNotContain(Api, references);
    }

    private static string[] ReferencedProjects(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => name is Domain or Application or Infrastructure or Api)
            .ToArray();
}
