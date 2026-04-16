using FluentValidation;
using ProjectName.Application.Identity.Models.Requests;

namespace ProjectName.Application.Identity.Validators.Users;

public class BlockUserRequestValidator : AbstractValidator<BlockUserRequest>
{
    public BlockUserRequestValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();
    }
}
