namespace ProjectName.Infrastructure.Configs;

public class BrevoConfig
{
    public string? Url { get; set; }
    public string? ApiKey { get; set; }
    public string FromEmail { get; set; } = null!;
    public string? FromEmailName { get; set; }
}