namespace ProjectName.Application.Identity.Models.Requests;

public record RegisterUserRequest(string Email, string Name, string Password, string ConfirmPassword);
