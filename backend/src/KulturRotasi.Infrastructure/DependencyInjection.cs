using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace KulturRotasi.Infrastructure;

public static class DependencyInjection
{
    public static readonly Assembly Assembly = typeof(DependencyInjection).Assembly;

    /// <summary>Registers the database, identity and other external services. Filled in from M1 onwards.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        return services;
    }
}
