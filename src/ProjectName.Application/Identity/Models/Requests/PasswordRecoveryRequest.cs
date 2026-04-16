namespace ProjectName.Application.Identity.Models.Requests;

public record PasswordRecoveryRequest(string Token, string Email, string Password, string ConfirmPassword);