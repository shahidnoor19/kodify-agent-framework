using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        builder.AddAIAgent("support", (sp, key) => Traced(sp, Client(sp).AsAIAgent(name: key, instructions: Instructions.Support)));
        builder.AddAIAgent("triage", (sp, key) => Traced(sp, Client(sp).AsAIAgent(name: key, instructions: Instructions.Triage)));
        return builder;
    }

    private static IChatClient Client(IServiceProvider sp, ModelTier tier = ModelTier.Fast) =>
        sp.GetRequiredService<FoundryClients>().ChatClient(tier);

    // Every agent run becomes a span in the Aspire dashboard. ServiceDefaults listens to the "SupportCopilot" source.
    private static AIAgent Traced(IServiceProvider sp, AIAgent agent) =>
        agent.AsBuilder().UseOpenTelemetry(sourceName: "SupportCopilot").Build(sp);
}
