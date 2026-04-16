using FluentValidation;
using ProjectName.Application.Common.Constants;
using ProjectName.Application.Identity.Models.Requests;

namespace ProjectName.Application.Identity.Validators.Users;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty();

        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(Regexes.PasswordRegex);

        RuleFor(request => request.ConfirmPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(Regexes.PasswordRegex)
            .Equal(request => request.NewPassword);
    }
}