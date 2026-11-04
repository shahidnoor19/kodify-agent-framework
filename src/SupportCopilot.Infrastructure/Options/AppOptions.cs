using System.ComponentModel.DataAnnotations;

namespace SupportCopilot.Infrastructure.Options;

/// <summary>Models deployed in Microsoft Foundry. The key lives in user secrets. Validated at startup.</summary>
public sealed class FoundryOptions
{
    public const string Section = "AzureFoundry";

    [Required, Url] public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";

    /// <summary>Fast, cheap model for chat, triage and specialists.</summary>
    [Required] public string ChatDeployment { get; set; } = "gpt-4.1-mini";

    /// <summary>Embedding model for the knowledge index (integrated vectorizer + seeding).</summary>
    public string EmbeddingDeployment { get; set; } = "text-embedding-3-small";

    /// <summary>Reasoning model for planners (harness, Magentic manager). Empty = use <see cref="ChatDeployment"/>.</summary>
    public string ReasoningDeployment { get; set; } = "";

    /// <summary>Foundry project endpoint for the Foundry Agent Service. Uses your az login (Entra ID).</summary>
    public string ProjectEndpoint { get; set; } = "";

    /// <summary>Optional vector store id for the File Search built-in tool.</summary>
    public string VectorStoreId { get; set; } = "";

    /// <summary>Use Entra ID (DefaultAzureCredential) instead of the API key.</summary>
    public bool UseEntraId { get; set; }

    /// <summary>Retries for 429 (throttling) and 5xx, with exponential back-off that honours Retry-After.</summary>
    [Range(0, 10)] public int MaxRetries { get; set; } = 5;

    /// <summary>Per call. Reasoning models can think for minutes on a long harness step, so keep this generous.</summary>
    [Range(10, 600)] public int NetworkTimeoutSeconds { get; set; } = 300;

    /// <summary>Log prompts and answers in traces. Development only: it puts personal data into telemetry.</summary>
    public bool LogSensitiveData { get; set; }
}

/// <summary>Where runtime data lives (database, checkpoints, harness notes), outside the source tree.</summary>
public sealed class StorageOptions
{
    public const string Section = "Storage";

    /// <summary>Relative paths are resolved against the app's content root.</summary>
    public string DataRoot { get; set; } = "App_Data";
}
