using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.Common.Abstractions;

public interface IMailService
{
    Task<Result> SendMailBrevoAsync(string to, string subject, string body, string? htmlBody = null);
}