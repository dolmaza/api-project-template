using Microsoft.OpenApi;
using ProjectName.Api.Endpoints.Identity;
using ProjectName.API.Endpoints.Identity;
using ProjectName.Azure.Infrastructure;
using ProjectName.Infrastructure.Database;

namespace ProjectName.API.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    extension(WebApplication? app)
    {
        public async Task InstallApplication(IConfiguration configuration, IWebHostEnvironment environment)
        {
            if (app == null)
            {
                return;
            }

            if (!environment.IsProduction())
            {
                app.ConfigureSwagger(configuration);
            }

            app.UseCors("CorsPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapDefaultRedirectEndpoint();

            app.MapIdentityEndpoints()
                .MapUserEndpoints()
                .MapAccountEndpoints();

            app.UseHttpsRedirection();

            await DatabaseInitializer.InitializeAsync(app);
            await AzureStorageInitializer.InitializeAsync(app);
        }

        private void MapDefaultRedirectEndpoint()
        {
            app?.MapGet("/", () => Results.Redirect(app.Environment.IsProduction() ? "/health" : "/swagger"))
                .ExcludeFromDescription();
        }

        private void ConfigureSwagger(IConfiguration configuration)
        {
            app.UseSwagger(o =>
            {
                o.PreSerializeFilters.Add((doc, req) =>
                {
                    doc.Servers = new List<OpenApiServer>
                    {
                        new OpenApiServer { Url = "/" }
                    };
                });
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "SercheMed API V1");
                options.DocumentTitle = "SercheMed API - Swagger UI";

                var clientId = configuration["SwaggerConfig:ClientId"] ?? "swagger-client";
                var clientSecret = configuration["SwaggerConfig:ClientSecret"] ?? "MAaeVV18Eq5Fse9";
                options.OAuthClientId(clientId);
                options.OAuthClientSecret(clientSecret);
                options.OAuthAppName("Swagger UI - SercheMed API");
                options.OAuthScopes("api", "offline_access");
                options.DisplayRequestDuration();
                options.EnableDeepLinking();
                options.EnableFilter();
                options.ShowExtensions();
                options.EnablePersistAuthorization();

                // Add request interceptor to inject custom headers for token requests
                options.UseRequestInterceptor("(req) => { " +
                                              "if (req.url.includes('/connect/token')) { " +
                                              "req.headers['requestId'] = crypto.randomUUID ? crypto.randomUUID() : Date.now().toString(); " +
                                              "} " +
                                              "return req; " +
                                              "}");
            });
        }
    }
}