using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace KulturRotasi.Application;

public static class DependencyInjection
{
    public static readonly Assembly Assembly = typeof(DependencyInjection).Assembly;

    /// <summary>Registers use-case handlers and validators. Filled in from M1 onwards.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
