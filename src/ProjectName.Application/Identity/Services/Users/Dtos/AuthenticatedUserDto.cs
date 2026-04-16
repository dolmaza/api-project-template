using ProjectName.Domain.AggregatesModel.IdentityAggregate;

namespace ProjectName.Application.Identity.Services.Users.Dtos;

public record AuthenticatedUserDto(string Id, string? Email, string Name, UserRole[] Roles);