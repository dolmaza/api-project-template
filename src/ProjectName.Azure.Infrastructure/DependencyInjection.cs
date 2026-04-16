using Azure;
using Azure.AI.OpenAI;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectName.Azure.Infrastructure.Configs;
using ProjectName.Azure.Infrastructure.Services;
using ProjectName.Application.Common.Abstractions;

namespace ProjectName.Azure.Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers Azure Storage services (Blob &amp; Queue) and Azure AI (MAF agents).
        /// Connection strings are resolved from Aspire-injected configuration ("blobs" / "queues").
        /// </summary>
        public IServiceCollection AddAzureInfrastructure(IConfiguration configuration)
        {
            services.ConfigureStorageClients(configuration)
                .AddStorageServices()
                .AddAiServices(configuration);

            return services;
        }

        private static string? NonEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;

        private IServiceCollection ConfigureStorageClients(IConfiguration configuration)
        {
            var blobConnectionString =
                NonEmpty(configuration.GetConnectionString("blobs"))
                ?? NonEmpty(configuration["blobs"])
                ?? throw new InvalidOperationException("Missing 'blobs' connection string.");
            var queueConnectionString =
                NonEmpty(configuration.GetConnectionString("queues"))
                ?? NonEmpty(configuration["queues"])
                ?? throw new InvalidOperationException("Missing 'queues' connection string.");;

            services.Configure<AzureStorageConfig>(config =>
            {
                config.BlobConnectionString = blobConnectionString;
                config.QueueConnectionString = queueConnectionString;
            });

            services.AddSingleton(new BlobServiceClient(blobConnectionString));
            services.AddSingleton(new QueueServiceClient(queueConnectionString, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.None
            }));

            return services;
        }

        private IServiceCollection AddStorageServices()
        {
            services.AddScoped<IFileStorageService, AzureBlobStorageService>();
            services.AddScoped<IQueueService, AzureQueueService>();

            return services;
        }

        private IServiceCollection AddAiServices(IConfiguration configuration)
        {
            var aiSettings = configuration.GetSection(AzureAiSettings.SectionName).Get<AzureAiSettings>()
                ?? throw new InvalidOperationException($"Missing '{AzureAiSettings.SectionName}' configuration section.");

            services.AddSingleton(aiSettings);

            return services;
        }
    }
}
