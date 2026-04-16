using Microsoft.AspNetCore.Identity;

namespace ProjectName.Application.Common.Extensions;

public static class IdentityResultExtensions
{
    public static string? ErrorsToString(this IdentityResult? identityResult)
    {
        return identityResult is { Succeeded: false } ? string.Join("\n", identityResult.Errors.Select(e => e.Description).ToList()) : null;
    }
}