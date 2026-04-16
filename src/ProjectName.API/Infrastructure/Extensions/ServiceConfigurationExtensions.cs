using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using ProjectName.API.Infrastructure.Middleware;
using ProjectName.Application.Common.Constants;
using ProjectName.Azure.Infrastructure;
using ProjectName.Domain.AggregatesModel.IdentityAggregate;
using ProjectName.Infrastructure;
using ProjectName.Infrastructure.Configs;
using ProjectName.Infrastructure.Database;

namespace ProjectName.API.Infrastructure.Extensions;

public static class ServiceConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureApiServices(IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            services.AddHttpContextAccessor()
                .AddExceptionHandler<GlobalExceptionHandler>()
                .ConfigureSwagger(configuration)
                .ConfigureIdentity(configuration, environment)
                .ConfigureAuthenticationAndAuthorization(configuration)
                .ConfigureCors(configuration, environment)
                .ConfigureOptions(configuration)
                .ConfigureEnumsAsStringInResponse();

            services.AddInfrastructure(configuration)
                .AddAzureInfrastructure(configuration);

            services.AddSignalR();

            return services;
        }

        private IServiceCollection ConfigureSwagger(IConfiguration configuration)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "SercheMed API",
                    Version = "v1"
                });

                // OAuth2 Password flow setup
                var authority = configuration["IdentityConfig:Authority"]?.TrimEnd('/') ?? string.Empty;
                var tokenUrlCfg = configuration["SwaggerConfig:TokenUrl"];
                var tokenUrl = !string.IsNullOrWhiteSpace(authority)
                    ? new Uri($"{authority}/api/identity/connect/token")
                    : (!string.IsNullOrWhiteSpace(tokenUrlCfg) ? new Uri(tokenUrlCfg, UriKind.RelativeOrAbsolute) : null);

                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        Password = tokenUrl != null ? new OpenApiOAuthFlow
                        {
                            TokenUrl = tokenUrl,
                            Scopes = new Dictionary<string, string>
                            {
                                { "api", "Access SercheMed API" },
                                { "offline_access", "Request refresh tokens" }
                            }
                        } : null
                    }
                });

                // Apply security requirement to all operations so the Authorize button injects Bearer token
                options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("oauth2"),
                        ["api", "offline_access"]
                    }
                });
            });
            return services;
        }

        private IServiceCollection ConfigureIdentity(IConfiguration configuration, IWebHostEnvironment environment)
        {
            // Add Identity with explicit password policy
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Password settings - match your validation requirements
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false; // Allow passwords without special characters
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;

                // SignIn settings
                options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedPhoneNumber = false;
                options.SignIn.RequireConfirmedAccount = false;

                // Token providers
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultProvider;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddTotpTokenProvider();

            // Configure OpenIddict
            services.AddOpenIddict()
                .AddCore(options =>
                {
                    options.UseEntityFrameworkCore()
                        .UseDbContext<ApplicationDbContext>();
                })
                .AddServer(options =>
                {
                    options.SetAuthorizationEndpointUris("/api/identity/connect/authorize")
                        .SetTokenEndpointUris("/api/identity/connect/token")
                        .SetUserInfoEndpointUris("/api/identity/connect/userinfo");

                    options.AllowAuthorizationCodeFlow();
                    options.AllowClientCredentialsFlow();
                    options.AllowPasswordFlow();
                    options.AllowRefreshTokenFlow();

                    options.RegisterScopes("api", OpenIddictConstants.Scopes.Email, OpenIddictConstants.Scopes.Profile, OpenIddictConstants.Scopes.Roles);

                    options.AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();

                    var aspNetCoreBuilder = options.UseAspNetCore()
                        .EnableTokenEndpointPassthrough()
                        .EnableAuthorizationEndpointPassthrough()
                        .EnableUserInfoEndpointPassthrough();

                    if (environment.IsDevelopment())
                    {
                        aspNetCoreBuilder.DisableTransportSecurityRequirement();
                    }

                    options.AddEncryptionKey(new SymmetricSecurityKey(Convert.FromBase64String(configuration["IdentityConfig:EncryptionKey"]!)));
                })
                .AddValidation(options =>
                {
                    options.UseLocalServer();
                    options.UseAspNetCore();
                });

            return services;
        }

        private IServiceCollection ConfigureAuthenticationAndAuthorization(IConfiguration configuration)
        {
            var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            });
            
            authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = configuration["IdentityConfig:Authority"];
                options.ClaimsIssuer = configuration["IdentityConfig:ClaimsIssuer"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false
                };
            });
            
            // Add Google OAuth
            var googleClientId = configuration["Authentication:Google:ClientId"];
            var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
            
            if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
            {
                authBuilder.AddGoogle("Google", options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    options.SaveTokens = true;
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                });
            }
            
            // Add Facebook OAuth
            var facebookAppId = configuration["Authentication:Facebook:AppId"];
            var facebookAppSecret = configuration["Authentication:Facebook:AppSecret"];
            
            if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
            {
                authBuilder.AddFacebook("Facebook", options =>
                {
                    options.AppId = facebookAppId;
                    options.AppSecret = facebookAppSecret;
                    options.SaveTokens = true;
                    options.Scope.Add("email");
                    options.Scope.Add("public_profile");
                    options.Fields.Add("name");
                    options.Fields.Add("email");
                });
            }

            services.AddAuthorizationBuilder()
                .AddPolicy(AuthorizationPolicyNames.AdministratorPolicy, policy =>
                    policy.RequireClaim(ClaimTypes.Role, RoleNames.Administrator))
                .AddPolicy(AuthorizationPolicyNames.CustomerPolicy, policy =>
                    policy.RequireClaim(ClaimTypes.Role, RoleNames.Customer));

            return services;
        }

        private IServiceCollection ConfigureCors(IConfiguration configuration, IWebHostEnvironment environment)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    // Use specific origins with credentials support for SignalR
                    var allowedOrigins = configuration.GetSection("Cors:AllowOrigins").Get<string[]>() ?? [];
                    
                    if (allowedOrigins.Length > 0)
                    {
                        builder.WithOrigins(allowedOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .WithExposedHeaders("Content-Description", "content-disposition")
                            .AllowCredentials();
                    }
                    else
                    {
                        // Fallback for environments without configuration (not recommended for SignalR)
                        builder.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                });
            });

            return services;
        }

        private IServiceCollection ConfigureOptions(IConfiguration configuration)
        {
            services.AddOptions();

            services.Configure<BrevoConfig>(configuration.GetSection(nameof(BrevoConfig)));

            return services;
        }

        private IServiceCollection ConfigureEnumsAsStringInResponse()
        {
            services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            return services;
        }

    }
}