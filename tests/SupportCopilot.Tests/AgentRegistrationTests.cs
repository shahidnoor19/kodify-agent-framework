using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SupportCopilot.Agents;
using SupportCopilot.Infrastructure;

namespace SupportCopilot.Tests;

/// <summary>Episode 8: the agents come from DI, registered once, sharing one Foundry client.</summary>
public sealed class AgentRegistrationTests
{
    /// <summary>The same registration as the web app, without the web server.</summary>
    private static IHost BuildHost(bool live = false)
    {
        var builder = Host.CreateApplicationBuilder();
        if (live) builder.Configuration.AddUserSecrets<AgentRegistrationTests>();
        else builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureFoundry:Endpoint"] = "https://example.openai.azure.com",
            ["AzureFoundry:ApiKey"] = "not-a-real-key",
        });
        builder.Services.AddInfrastructure(builder.Configuration, AppContext.BaseDirectory);
        builder.AddSupportAgents();
        return builder.Build();
    }

    [Fact]
    public void Agents_are_keyed_singletons()
    {
        using var host = BuildHost();

        var first = host.Services.GetRequiredKeyedService<AIAgent>("triage");
        var second = host.Services.GetRequiredKeyedService<AIAgent>("triage");

        Assert.Same(first, second);
        Assert.Equal("triage", first.Name);
    }

    [LiveFact]
    public async Task Triage_agent_answers_through_the_shared_client()
    {
        using var host = BuildHost(live: true);
        var triage = host.Services.GetRequiredKeyedService<AIAgent>("triage");

        var response = await triage.RunAsync("I was charged twice for order 1002!");

        Assert.Contains("Billing", response.Text, StringComparison.OrdinalIgnoreCase);
    }
}
