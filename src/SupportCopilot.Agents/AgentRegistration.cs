using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SupportCopilot.Agents.Conversations;
using SupportCopilot.Agents.Middleware;
using SupportCopilot.Agents.Memory;
using SupportCopilot.Agents.Tools;
using SupportCopilot.Infrastructure.Foundry;

namespace SupportCopilot.Agents;

/// <param name="RunsAsJob">Long-running agents run in the background worker, not inside the HTTP request.</param>
public sealed record AgentInfo(string Key, string Title, string Description, ModelTier Model, bool RunsAsJob = false);

/// <summary>Registers every agent in DI as a keyed AIAgent singleton. Each agent declares its model tier.</summary>
public static class AgentRegistration
{
    public static readonly List<AgentInfo> Catalog =
    [
        new("support", "Support Agent", "Tools, approvals, memory, images, guardrails", ModelTier.Fast),
        new("triage", "Triage", "Classifies tickets", ModelTier.Fast),
    ];

    public static IHostApplicationBuilder AddSupportAgents(this IHostApplicationBuilder builder)
    {
        builder.AddAIAgent("support", (sp, key) => Guarded(sp, ModelTier.Fast, Client(sp).AsAIAgent(new ChatClientAgentOptions
        {
            Name = key,
            Description = "Kodify Store customer support agent",
            ChatOptions = new ChatOptions
            {
                Instructions = Instructions.Support,
                // The tools the model may call. Read tools run freely; refund and cancel wait for a human to approve.
                Tools = [.. sp.GetRequiredService<OrderTools>().AllTools(), .. sp.GetRequiredService<TicketTools>().AllTools()],
            },
            // Tells the agent who it talks to, before every run.
            AIContextProviders = [sp.GetRequiredService<UserMemoryProvider>()],
            // Keep the last 30 messages: a long chat does not grow the context window, and the cost, forever.
            ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
            {
                ChatReducer = new MessageCountingChatReducer(30),
            }),
        })));
        builder.AddAIAgent("triage", (sp, key) => Guarded(sp, ModelTier.Fast, Client(sp).AsAIAgent(name: key, instructions: Instructions.Triage)));
        // The conversation store reads and writes the database, so it lives per request (scoped).
        builder.Services.AddScoped<ConversationStore>();
        builder.Services.AddScoped<ApprovalStore>();
        // Tools are singletons, like the agents; every call opens its own scope through ToolScope.
        builder.Services.AddSingleton<ToolScope>();
        builder.Services.AddSingleton<OrderTools>();
        builder.Services.AddSingleton<TicketTools>();
        builder.Services.AddSingleton<UserMemoryProvider>();
        // The guardrails around every agent: PII redaction, tool logging, token usage and the OpenTelemetry span.
        builder.Services.AddSingleton<PiiRedactor>();
        builder.Services.AddSingleton<AgentGuardrails>();
        return builder;
    }

    private static IChatClient Client(IServiceProvider sp, ModelTier tier = ModelTier.Fast) =>
        sp.GetRequiredService<FoundryClients>().ChatClient(tier);

    // Every agent goes through the same guardrails; the model name of its tier is recorded with the usage.
    private static AIAgent Guarded(IServiceProvider sp, ModelTier tier, AIAgent agent) =>
        sp.GetRequiredService<AgentGuardrails>().Apply(agent, sp, sp.GetRequiredService<FoundryClients>().Model(tier));
}
