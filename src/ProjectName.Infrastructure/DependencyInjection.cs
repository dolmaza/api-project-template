using System.Reflection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectName.Application;
using ProjectName.Application.Common.Abstractions;
using ProjectName.Application.Common.Configs;
using ProjectName.Application.Identity.Services.Account;
using ProjectName.Application.Identity.Services.Users;
using ProjectName.Domain.Common.Abstractions;
using ProjectName.Domain.Common.Mediator.Extensions;
using ProjectName.Domain.Common.Services;
using ProjectName.Domain.SeedWork;
using ProjectName.Infrastructure.Behaviours;
using ProjectName.Infrastructure.Database;
using ProjectName.Infrastructure.Idempotency;
using ProjectName.Infrastructure.Repositories;
using ProjectName.Infrastructure.Services;

namespace ProjectName.Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration, bool includeIdentityServices = true)
        {
            services.AddCurrentState()
                .ConfigureDatabase(configuration)
                .AddCustomServices(includeIdentityServices);

            return services;
        }

        private IServiceCollection AddCurrentState()
        {
            services.AddScoped<ICurrentStateService, CurrentStateService>();

            return services;
        }

        private static string? NonEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;

        private IServiceCollection ConfigureDatabase(IConfiguration configuration)
        {
            var connectionString =
                NonEmpty(configuration.GetConnectionString("serche-med-db"))
                ?? NonEmpty(configuration["serche-med-db"])
                ?? throw new InvalidOperationException("Missing 'serche-med-db' connection string.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.Configure<ConnectionStringsConfig>(config =>
            {
                config.DefaultConnection = connectionString;
            });

            return services;
        }

        private IServiceCollection AddCustomServices(bool includeIdentityServices = true)
        {
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

            services.AddScoped<IRequestManager, RequestManager>();

            if (includeIdentityServices)
            {
                services.AddScoped<IAccountService, AccountService>();
                services.AddScoped<IUserService, UserService>();
                services.AddScoped<IMailService, MailService>();
            }

            services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyReference>();

            services.AddAllQueryExecutors(typeof(ApplicationAssemblyReference).Assembly);

            services.AddMediator(typeof(DependencyInjection).Assembly, typeof(ApplicationAssemblyReference).Assembly)
                .AddMediatorPipeline(typeof(LoggingBehavior<,>))
                .AddMediatorPipeline(typeof(ValidatorBehavior<,>))
                .AddMediatorPipeline(typeof(TransactionBehaviour<,>));

            return services;
        }

        private void AddAllQueryExecutors(Assembly assembly)
        {
            var interfaceType = typeof(IQueryExecutor<,>);

            var types = assembly
                .GetTypes()
                .Where(type => type is { IsAbstract: false, IsInterface: false })
                .SelectMany(type =>
                    type.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType)
                        .Select(i => new { Interface = i, Implementation = type }))
                .ToList();

            foreach (var type in types)
            {
                services.AddScoped(type.Implementation);
            }
        }
    }
}