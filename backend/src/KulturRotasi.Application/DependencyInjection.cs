using System.Reflection;
using FluentValidation;
using KulturRotasi.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace KulturRotasi.Application;

public static class DependencyInjection
{
    public static readonly Assembly Assembly = typeof(DependencyInjection).Assembly;

    /// <summary>Registers every query handler (wrapped in validation) and every validator in this assembly.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly);

        var handlers = Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericType: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                .Select(contract => (Implementation: type, Contract: contract)));

        foreach (var (implementation, contract) in handlers)
        {
            // Asking for IQueryHandler<TQuery, TResult> returns the handler wrapped in a ValidationDecorator
            var decorator = typeof(ValidationDecorator<,>).MakeGenericType(contract.GetGenericArguments());
            services.AddScoped(implementation);
            services.AddScoped(contract, provider =>
                ActivatorUtilities.CreateInstance(provider, decorator, provider.GetRequiredService(implementation)));
        }

        return services;
    }
}
