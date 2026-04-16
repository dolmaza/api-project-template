
using brevo_csharp.Api;
using brevo_csharp.Model;
using Microsoft.Extensions.Options;
using ProjectName.Application.Common.Abstractions;
using ProjectName.Domain.Common.ResultPattern;
using ProjectName.Infrastructure.Configs;
using Configuration = brevo_csharp.Client.Configuration;

namespace ProjectName.Infrastructure.Services;

public class MailService(IOptions<BrevoConfig> brevoConfig) : IMailService
{
    private readonly BrevoConfig _brevoConfig = brevoConfig.Value;
    public async Task<Result> SendMailBrevoAsync(string to, string subject, string body, string? htmlBody = null)
    {
        Configuration.Default.AddApiKey("api-key", _brevoConfig.ApiKey);

        var apiInstance = new TransactionalEmailsApi();
        var sendSmtpEmail = new SendSmtpEmail
        (
            to: [new SendSmtpEmailTo(to)],
            sender: new SendSmtpEmailSender(_brevoConfig.FromEmailName, _brevoConfig.FromEmail),
            subject: subject,
            htmlContent: htmlBody,
            textContent: body
        );

        try
        {
            await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(Error.Failure("Mailing.SendBrevoMail", $"Exception when calling TransactionalEmailsApi.SendTransacEmail: {e.Message}"));
        }
    }
}