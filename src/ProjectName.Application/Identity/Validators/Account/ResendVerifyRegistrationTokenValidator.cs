using FluentValidation;
using ProjectName.Application.Identity.Models.Requests;

namespace ProjectName.Application.Identity.Validators.Account;

public class ResendVerifyRegistrationTokenValidator : AbstractValidator<ResendVerificationTokenRequest>
{
    public ResendVerifyRegistrationTokenValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty();
    }
}