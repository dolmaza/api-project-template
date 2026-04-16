using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using ProjectName.Domain.Common.Abstractions;

namespace ProjectName.Domain.Common.Services;

public class CurrentStateService(IHttpContextAccessor context) : ICurrentStateService
{
    public string GetAuthorizedId()
    {
        return context.HttpContext?.User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value ?? "anonymous";
    }

    public bool IsInRole(string roleName)
    {
        return context.HttpContext?.User.IsInRole(roleName) ?? false;
    }

    public Guid GetCorrelationIdFromHeader()
    {
        if (context.HttpContext?.Request?.Headers?.TryGetValue("requestId", out var correlationId) ?? false)
        {
            return Guid.TryParse(correlationId, out var guidCorrelationId) ? guidCorrelationId : Guid.NewGuid();
        }

        return Guid.NewGuid();
    }
}