namespace ProjectName.Azure.Infrastructure.Configs;

/// <summary>
/// Per-agent Azure OpenAI constraints.
/// Each property maps directly to a <see cref="Microsoft.Extensions.AI.ChatOptions"/> field,
/// giving independent cost and quality control for every pipeline stage.
/// </summary>
public sealed class PrescriptionAgentSettings
{
    /// <summary>
    /// Azure OpenAI deployment name for this agent (e.g. "gpt-4o" or "gpt-4o-mini").
    /// Use a vision-capable model (gpt-4o) for OcrVisionAgent; cheaper models suffice for text-only agents.
    /// </summary>
    public string DeploymentName { get; set; } = "gpt-4o";

    /// <summary>
    /// Hard cap on generated tokens — the primary cost lever.
    /// Set this as low as the expected output allows to avoid paying for padding.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 1000;

    /// <summary>
    /// Sampling temperature. 0 = fully deterministic — ideal for structured extraction.
    /// Never raise above 0.2 for medical data pipelines.
    /// </summary>
    public float Temperature { get; set; } = 0f;
}
