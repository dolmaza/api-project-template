namespace ProjectName.Application.Identity.Models.Requests;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);