using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using SupportCopilot.Infrastructure.Options;

namespace SupportCopilot.Infrastructure.Foundry;

/// <summary>Which model an agent needs. Every agent declares one, so cost and quality are a conscious choice.</summary>
public enum ModelTier
{
    /// <summary>Cheap and fast: chat, triage, specialists.</summary>
    Fast,
    /// <summary>Plans multi-step work: harness, Magentic manager.</summary>
    Reasoning,
}

/// <summary>
/// One OpenAI client for the Foundry v1 endpoint, shared by every agent.
/// Retries 429/5xx with back-off (System.ClientModel honours Retry-After) and has a network timeout.
/// </summary>
public sealed class FoundryClients
{
    private readonly OpenAIClient _client;

    public FoundryClients(IOptions<FoundryOptions> options)
    {
        Options = options.Value;

        // https://<resource>.openai.azure.com/openai/v1/
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(new Uri(Options.Endpoint), "/openai/v1/"),
            RetryPolicy = new ClientRetryPolicy(Options.MaxRetries),
            NetworkTimeout = TimeSpan.FromSeconds(Options.NetworkTimeoutSeconds),
        };

        _client = Options.UseEntraId
            ? new OpenAIClient(new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"), clientOptions)
            : new OpenAIClient(new ApiKeyCredential(Options.ApiKey), clientOptions);
    }

    public FoundryOptions Options { get; }

    /// <summary>The deployment name for a tier. The reasoning tier falls back to the chat model until one is deployed.</summary>
    public string Model(ModelTier tier) =>
        tier == ModelTier.Reasoning && !string.IsNullOrWhiteSpace(Options.ReasoningDeployment)
            ? Options.ReasoningDeployment
            : Options.ChatDeployment;

    /// <summary>Chat Completions API as an IChatClient.</summary>
    public IChatClient ChatClient(ModelTier tier = ModelTier.Fast) => _client.GetChatClient(Model(tier)).AsIChatClient();
}
