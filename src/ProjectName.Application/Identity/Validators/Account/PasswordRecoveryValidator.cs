using FluentValidation;
using ProjectName.Application.Common.Constants;
using ProjectName.Application.Identity.Models.Requests;

namespace ProjectName.Application.Identity.Validators.Account;

public class PasswordRecoveryValidator : AbstractValidator<PasswordRecoveryRequest>
{
    public PasswordRecoveryValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Token)
            .NotEmpty();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(Regexes.PasswordRegex);

        RuleFor(request => request.ConfirmPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(Regexes.PasswordRegex)
            .Equal(request => request.Password);
    }
}