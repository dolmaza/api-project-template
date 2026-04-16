using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ProjectName.Domain.Common.Mediator.Abstractions;

namespace ProjectName.Domain.Common.Mediator.Extensions;

public static class MediatorServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMediator(params Assembly[] assemblies)
        {
            // Register mediator
            services.AddScoped<IMediator, Mediator>();

            // Register handlers
            var handlerTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                    .Select(i => new { Handler = t, Interface = i }));

            foreach (var h in handlerTypes)
                services.AddScoped(h.Interface, h.Handler);

            return services;
        }

        public IServiceCollection AddMediatorPipeline(Type pipeline)
        {
            if (!pipeline.IsGenericType || pipeline.GetInterfaces().FirstOrDefault()?.Name != typeof(IPipelineBehavior<,>).Name)
                throw new ArgumentException("Pipeline must be a generic type implementing IPipelineBehavior<,>", nameof(pipeline));

            services.AddScoped(typeof(IPipelineBehavior<,>),pipeline);

            return services;
        }
    }
}
