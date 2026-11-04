using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SupportCopilot.Infrastructure.Foundry;

namespace SupportCopilot.Agents;

/// <summary>Registers every agent in DI as a keyed AIAgent singleton. Each agent declares its model tier.</summary>
public static class AgentRegistration
{
    public static IHostApplicationBuilder AddSupportAgents(this IHostApplicationBuilder builder)
    {
        builder.AddAIAgent("support", (sp, key) => Client(sp).AsAIAgent(name: key, instructions: Instructions.Support));
        builder.AddAIAgent("triage", (sp, key) => Client(sp).AsAIAgent(name: key, instructions: Instructions.Triage));
        return builder;
    }

    private static IChatClient Client(IServiceProvider sp, ModelTier tier = ModelTier.Fast) =>
        sp.GetRequiredService<FoundryClients>().ChatClient(tier);
}
