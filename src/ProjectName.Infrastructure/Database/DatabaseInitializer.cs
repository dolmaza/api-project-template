using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using ProjectName.Domain.AggregatesModel.IdentityAggregate;

namespace ProjectName.Infrastructure.Database;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IApplicationBuilder app)
    {
        MigrateDatabase(app);

        await SeedDataAsync(app);
    }

    private static async Task SeedDataAsync(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        const string adminEmail = "admin@example.com";
        const string mobileClientName = "mobile-client";
        const string swaggerClientName = "swagger-client";
        const string webClientName = "web-client";

        var roleNames = Enum.GetNames<UserRole>().ToList();

        foreach (var role in from roleName in roleNames
                             where !applicationDbContext.Roles
                     .Any(r => r.NormalizedName == roleName.ToUpper())
                             select new IdentityRole(roleName))
        {
            role.NormalizedName = role.Name?.ToUpper();
            applicationDbContext.Roles.Add(role);
            await applicationDbContext.SaveChangesAsync();
        }

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var user = new ApplicationUser { UserName = "admin", Email = adminEmail, Name = "Admin User", EmailConfirmed = true, LockoutEnabled = false, LockoutEnd = null };
            await userManager.CreateAsync(user, "Admin@123");

            await userManager.AddToRoleAsync(user, nameof(UserRole.Administrator));
        }

        if (await appManager.FindByClientIdAsync(mobileClientName) == null)
        {
            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = mobileClientName,
                ClientSecret = "MAaeVV18Eq5Fse9",
                DisplayName = "Mobile Client",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                }
            });
        }

        if (await appManager.FindByClientIdAsync(swaggerClientName) == null)
        {
            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = swaggerClientName,
                ClientSecret = "MAaeVV18Eq5Fse9",
                DisplayName = "Swagger UI Client",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api"
                }
            });
        }

        if (await appManager.FindByClientIdAsync(webClientName) == null)
        {
            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = webClientName,
                ClientSecret = "MAaeVV18Eq5Fse9",
                DisplayName = "React Web Client",
                RedirectUris =
                {
                    new Uri("http://localhost:8080/auth/callback"),
                    new Uri("http://localhost:8080/silent-renew")
                },
                PostLogoutRedirectUris =
                {
                    new Uri("http://localhost:8080")
                },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api"
                }
            });
        }
    }

    private static void MigrateDatabase(IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        serviceScope.ServiceProvider.GetService<ApplicationDbContext>()?.Database.Migrate();
    }
}