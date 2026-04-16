using Microsoft.AspNetCore.Identity;
using ProjectName.Application.Common.Providers;
using ProjectName.Domain.AggregatesModel.IdentityAggregate;

namespace ProjectName.API.Infrastructure.Extensions;

public static class IdentityBuilderExtensions
{
    public static IdentityBuilder AddTotpTokenProvider(this IdentityBuilder builder)
    {
        var userType = builder.UserType;
        var totpProvider = typeof(TotpTokenProvider<>).MakeGenericType(userType);
        return builder.AddTokenProvider(nameof(TotpTokenProvider<ApplicationUser>), totpProvider);
    }
}