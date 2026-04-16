using Microsoft.AspNetCore.Identity;

namespace ProjectName.Application.Common.Providers;

public class TotpTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser> where TUser : class
{
    public override Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
    {
        return Task.FromResult(false);
    }

    public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        var isValid = await base.ValidateAsync(purpose, token, manager, user);

        return isValid;
    }
}
