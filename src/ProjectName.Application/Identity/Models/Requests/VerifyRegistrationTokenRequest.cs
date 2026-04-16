namespace ProjectName.Application.Identity.Models.Requests;

public record VerifyRegistrationTokenRequest(string Email, string Otp);