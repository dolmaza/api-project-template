using FluentValidation;
using ProjectName.Application.Identity.Models.Requests;

namespace ProjectName.Application.Identity.Validators.Users;

public class ActivateUserRequestValidator : AbstractValidator<ActivateUserRequest>
{
    public ActivateUserRequestValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();
    }
}
