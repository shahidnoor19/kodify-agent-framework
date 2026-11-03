using System.Diagnostics;
using Microsoft.Agents.AI;
using OpenAI.Chat;

namespace AgentBasics.Demos;

/// <summary>Episode 5: the same task on a fast model and a reasoning model: answer, time, tokens and cost.</summary>
public static class ModelsAndCost
{
    /// <summary>Which model an agent needs. Every agent declares a tier, so cost and quality are a conscious choice.</summary>
    public enum ModelTier { Fast, Reasoning }

    // List prices in US dollars per million tokens (input, output). They change: check the Azure pricing page.
    private static readonly Dictionary<ModelTier, (decimal Input, decimal Output)> Prices = new()
    {
        [ModelTier.Fast] = (0.40m, 1.60m),
        [ModelTier.Reasoning] = (0.25m, 2.00m),
    };

    public static string Deployment(ModelTier tier) => tier == ModelTier.Reasoning
        ? Foundry.Config["AzureFoundry:ReasoningDeployment"] ?? "gpt-5-mini"
        : Foundry.ChatDeployment;

    public static decimal Cost(ModelTier tier, AgentResponse response)
    {
        var price = Prices[tier];
        return ((response.Usage?.InputTokenCount ?? 0) * price.Input + (response.Usage?.OutputTokenCount ?? 0) * price.Output) / 1_000_000m;
    }

    public static async Task RunAsync()
    {
        const string task = "A customer was charged twice for order 1002 and wants the money back today. Which team should handle it, and what are the first two steps?";

        foreach (var tier in new[] { ModelTier.Fast, ModelTier.Reasoning })
        {
            AIAgent agent = Foundry.CreateClient().GetChatClient(Deployment(tier)).AsAIAgent(
                name: $"{tier}Agent",
                instructions: "You are a support lead for Kodify Store. Answer in at most three sentences.");

            var clock = Stopwatch.StartNew();
            AgentResponse response = await agent.RunAsync(task);
            clock.Stop();

            Console.WriteLine($"[{tier}] {Deployment(tier)}");
            Console.WriteLine(response.Text);
            Console.WriteLine($"  time {clock.Elapsed.TotalSeconds:0.0}s, input {response.Usage?.InputTokenCount}, output {response.Usage?.OutputTokenCount}");
            Console.WriteLine($"  cost ${Cost(tier, response):0.000000}\n");
        }
    }
}
