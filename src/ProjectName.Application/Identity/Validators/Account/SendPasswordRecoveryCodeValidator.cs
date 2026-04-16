using FluentValidation;
using ProjectName.Application.Identity.Models.Requests;

namespace ProjectName.Application.Identity.Validators.Account;

public class SendPasswordRecoveryCodeValidator : AbstractValidator<SendPasswordRecoveryCodeRequest>
{
    public SendPasswordRecoveryCodeValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();
    }
}