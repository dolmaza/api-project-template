using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using ProjectName.Domain.AggregatesModel.IdentityAggregate;

namespace ProjectName.API.Endpoints.Identity;

public static class AuthorizationEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity")
            .WithTags("Identity");

        group.MapGet("/connect/authorize", HandleAuthorizeEndpoint)
            .WithName("Authorize")
            .AllowAnonymous();

        group.MapPost("/connect/authorize", HandleAuthorizeEndpoint)
            .WithName("AuthorizePost")
            .AllowAnonymous();

        group.MapGet("/connect/userinfo", HandleUserInfoEndpoint)
            .WithName("UserInfo")
            .RequireAuthorization();

        group.MapPost("/connect/userinfo", HandleUserInfoEndpoint)
            .WithName("UserInfoPost")
            .RequireAuthorization();

        group.MapPost("/connect/token", HandleTokenEndpoint)
            .WithName("Token")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> HandleTokenEndpoint(
        HttpContext httpContext,
        IOpenIddictApplicationManager applicationManager,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        var request = httpContext.GetOpenIddictServerRequest();
        if (request == null)
        {
            return Results.BadRequest("The OpenID Connect request cannot be retrieved.");
        }

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordGrantType(request, signInManager, userManager);
        }
        else if (request.IsAuthorizationCodeGrantType())
        {
            return await HandleAuthorizationCodeGrantType(httpContext, signInManager, userManager);
        }
        else if (request.IsClientCredentialsGrantType())
        {
            return await HandleClientCredentialsGrantType(request, applicationManager);
        }

        return Results.Problem("The specified grant type is not implemented.", statusCode: 400);
    }

    private static async Task<IResult> HandlePasswordGrantType(
        OpenIddictRequest request,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Username!);

        if (user == null)
        {
            return Results.Problem("Invalid username or password", statusCode: 400);
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password!, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Results.Problem("Invalid username or password", statusCode: 400);
        }

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        principal.SetScopes(request.GetScopes());
        principal.AddClaim(OpenIddictConstants.Claims.Subject, user.Id);

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> HandleAuthorizationCodeGrantType(
        HttpContext httpContext,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        var claimsPrincipal = (await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
        if (claimsPrincipal == null)
        {
            return Results.Problem("The authorization code is invalid or has expired.", statusCode: 400);
        }

        var userId = claimsPrincipal.GetClaim(OpenIddictConstants.Claims.Subject);
        var user = await userManager.FindByIdAsync(userId!);
        if (user == null)
        {
            return Results.Problem("The user associated with this authorization code cannot be found.", statusCode: 400);
        }

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        principal.AddClaim(OpenIddictConstants.Claims.Subject, user.Id);
        principal.SetScopes(claimsPrincipal.GetScopes());

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> HandleClientCredentialsGrantType(
        OpenIddictRequest request,
        IOpenIddictApplicationManager applicationManager)
    {
        var application = await applicationManager.FindByClientIdAsync(request.ClientId!);

        if (application == null)
        {
            return Results.Problem("The application cannot be found.", statusCode: 400);
        }

        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, request.ClientId!));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, 
            await applicationManager.GetDisplayNameAsync(application) ?? request.ClientId!));

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> HandleAuthorizeEndpoint(
        HttpContext httpContext,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        var request = httpContext.GetOpenIddictServerRequest();
        if (request == null)
        {
            return Results.BadRequest("The OpenID Connect request cannot be retrieved.");
        }

        var provider = httpContext.Request.Query["provider"].ToString();
        if (string.IsNullOrEmpty(provider))
        {
            provider = "Google";
        }

        if (provider != "Google" && provider != "Facebook")
        {
            return Results.BadRequest("Invalid authentication provider. Supported providers: Google, Facebook");
        }

        var authenticateResult = await httpContext.AuthenticateAsync(provider);
        
        if (!authenticateResult.Succeeded)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = httpContext.Request.PathBase + httpContext.Request.Path + httpContext.Request.QueryString
            };
            return Results.Challenge(properties, [provider]);
        }

        var user = await GetOrCreateUserFromExternalProvider(
            authenticateResult.Principal,
            provider,
            userManager);

        if (user.IsFailure)
        {
            return Results.Problem(user.Error, statusCode: user.StatusCode);
        }

        return await CreateAuthorizationResponse(
            user.User!,
            request,
            applicationManager,
            authorizationManager,
            scopeManager,
            signInManager);
    }

    private static async Task<(bool IsFailure, string? Error, int StatusCode, ApplicationUser? User)> GetOrCreateUserFromExternalProvider(
        ClaimsPrincipal? externalUser,
        string provider,
        UserManager<ApplicationUser> userManager)
    {
        var email = externalUser?.FindFirstValue(ClaimTypes.Email);
        var name = externalUser?.FindFirstValue(ClaimTypes.Name);
        var externalUserId = externalUser?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(email))
        {
            return (true, $"Email not provided by {provider}", 400, null);
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Name = name ?? email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return (true, "Failed to create user", 500, null);
            }

            await userManager.AddLoginAsync(user, new UserLoginInfo(provider, externalUserId!, provider));
            await userManager.AddToRoleAsync(user, nameof(UserRole.Customer));
        }
        else
        {
            var logins = await userManager.GetLoginsAsync(user);
            if (!logins.Any(l => l.LoginProvider == provider && l.ProviderKey == externalUserId))
            {
                await userManager.AddLoginAsync(user, new UserLoginInfo(provider, externalUserId!, provider));
            }
        }

        return (false, null, 200, user);
    }

    private static async Task<IResult> CreateAuthorizationResponse(
        ApplicationUser user,
        OpenIddictRequest request,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<ApplicationUser> signInManager)
    {
        var application = await applicationManager.FindByClientIdAsync(request.ClientId!);
        if (application == null)
        {
            return Results.Problem("The application cannot be found.", statusCode: 400);
        }

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        
        var identity = (ClaimsIdentity)principal.Identity!;
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id));

        var scopes = request.GetScopes();
        principal.SetScopes(scopes);
        principal.SetResources(await scopeManager.ListResourcesAsync(scopes).ToListAsync());

        var authorizationId = await authorizationManager.CreateAsync(
            principal: principal,
            subject: user.Id,
            client: (await applicationManager.GetIdAsync(application))!,
            type: OpenIddictConstants.AuthorizationTypes.Permanent,
            scopes: scopes);

        principal.SetAuthorizationId(await authorizationManager.GetIdAsync(authorizationId));

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> HandleUserInfoEndpoint(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager)
    {
        var claimsPrincipal = (await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
        if (claimsPrincipal == null)
        {
            return Results.Challenge();
        }

        var userId = claimsPrincipal.GetClaim(OpenIddictConstants.Claims.Subject);
        var user = await userManager.FindByIdAsync(userId!);
        if (user == null)
        {
            return Results.Problem("The user cannot be found.", statusCode: 404);
        }

        var claims = new Dictionary<string, object>
        {
            [OpenIddictConstants.Claims.Subject] = user.Id,
            [OpenIddictConstants.Claims.Email] = user.Email!,
            [OpenIddictConstants.Claims.Name] = user.Name,
            ["email_verified"] = user.EmailConfirmed
        };

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Any())
        {
            claims[OpenIddictConstants.Claims.Role] = roles;
        }

        return Results.Ok(claims);
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Subject:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case OpenIddictConstants.Claims.Name:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (principal.HasScope(OpenIddictConstants.Permissions.Scopes.Profile))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Email:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (principal.HasScope(OpenIddictConstants.Permissions.Scopes.Email))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (principal.HasScope(OpenIddictConstants.Permissions.Scopes.Roles))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
}