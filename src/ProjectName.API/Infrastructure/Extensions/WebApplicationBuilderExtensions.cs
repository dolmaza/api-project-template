using Serilog;

namespace ProjectName.API.Infrastructure.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static void ConfigureLogs(this WebApplicationBuilder? builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        var loggerConfiguration = new LoggerConfiguration()
            .Enrich.WithCorrelationIdHeader("requestId")
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}");

        var logger = loggerConfiguration.CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(logger);
    }
}