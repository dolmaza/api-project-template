namespace ProjectName.Application.Identity.Models.Requests;

public record VerifyPasswordRecoveryOtpRequest(string Email, string Otp);