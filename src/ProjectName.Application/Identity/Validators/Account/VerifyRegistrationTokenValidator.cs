using FluentValidation;
using ProjectName.Application.Identity.Models.Requests;

namespace ProjectName.Application.Identity.Validators.Account;

public class VerifyRegistrationTokenValidator : AbstractValidator<VerifyRegistrationTokenRequest>
{
    public VerifyRegistrationTokenValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty();

        RuleFor(request => request.Otp)
            .NotEmpty();
    }
}