namespace ProjectName.Azure.Infrastructure.Configs;

/// <summary>
/// Azure OpenAI connection settings and per-agent configuration for <see cref="Ai.PrescriptionAgentFactory"/>.
/// Bound from the "AzureAi" configuration section.
/// </summary>
public sealed class AzureAiSettings
{
    public const string SectionName = "AzureAi";

    /// <summary>Azure OpenAI resource endpoint, e.g. https://&lt;resource&gt;.openai.azure.com/</summary>
    public required string Endpoint { get; set; }

    public required string ApiKey { get; set; }

    /// <summary>
    /// OCR/Vision agent — must use a vision-capable deployment (e.g. gpt-4o).
    /// Needs higher token budget because extracted prescription text can be verbose.
    /// </summary>
    public PrescriptionAgentSettings OcrVisionAgent { get; set; } = new()
    {
        DeploymentName  = "gpt-4o",
        MaxOutputTokens = 2048,
        Temperature     = 0f
    };

    /// <summary>
    /// Extractor agent — identifies medicine lines and outputs a bounded JSON array.
    /// A tight token cap is safe because the output is structured and predictable.
    /// </summary>
    public PrescriptionAgentSettings ExtractorAgent { get; set; } = new()
    {
        DeploymentName  = "gpt-4o",
        MaxOutputTokens = 600,
        Temperature     = 0f
    };

    /// <summary>
    /// Validator agent — cleans and normalises an existing JSON array.
    /// Can use a cheaper model (gpt-4o-mini) because no vision or complex reasoning is needed.
    /// </summary>
    public PrescriptionAgentSettings ValidatorAgent { get; set; } = new()
    {
        DeploymentName  = "gpt-4o",
        MaxOutputTokens = 600,
        Temperature     = 0f
    };

    /// <summary>
    /// Keyword generator agent — converts a prescription item into 1–5 targeted search queries.
    /// </summary>
    public PrescriptionAgentSettings KeywordGeneratorAgent { get; set; } = new()
    {
        DeploymentName  = "gpt-4o",
        MaxOutputTokens = 200,
        Temperature     = 0f
    };

    /// <summary>
    /// Product validator agent — filters external search results to confirmed matches.
    /// </summary>
    public PrescriptionAgentSettings ProductValidatorAgent { get; set; } = new()
    {
        DeploymentName  = "gpt-4o",
        MaxOutputTokens = 300,
        Temperature     = 0f
    };
}
