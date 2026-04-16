using FluentValidation;
using ProjectName.Application.Identity.Models.Requests;

namespace ProjectName.Application.Identity.Validators.Users;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();

        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

    }
}
