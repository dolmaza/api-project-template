using FluentValidation;
using ProjectName.Application.Identity.Models.Requests;

namespace ProjectName.Application.Identity.Validators.Account;

public class VerifyPasswordRecoveryOtpValidator : AbstractValidator<VerifyPasswordRecoveryOtpRequest>
{
    public VerifyPasswordRecoveryOtpValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Otp)
            .NotEmpty();
    }
}