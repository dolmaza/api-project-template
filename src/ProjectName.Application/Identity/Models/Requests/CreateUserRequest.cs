using ProjectName.Domain.AggregatesModel.IdentityAggregate;

namespace ProjectName.Application.Identity.Models.Requests;

public record CreateUserRequest(string Email, string Name, string Password, string ConfirmPassword, UserRole Role);
