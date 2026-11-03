using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using OpenAI.Responses;

namespace AgentBasics.Demos;

/// <summary>Episode 3: IChatClient is the common layer under every agent. Three ways to get an agent from Foundry.</summary>
public static class ChatClients
{
    public static async Task RunAsync()
    {
        var foundry = Foundry.CreateClient();

        // 1. A plain IChatClient (Microsoft.Extensions.AI): no agent, just a model call.
        IChatClient chatClient = foundry.GetChatClient(Foundry.ChatDeployment).AsIChatClient();
        ChatResponse reply = await chatClient.GetResponseAsync("Say hello in one short sentence.");
        Console.WriteLine($"IChatClient   : {reply.Text}");

        // 2. Any IChatClient becomes an agent with ChatClientAgent.
        AIAgent agent = new ChatClientAgent(chatClient,
            instructions: "You answer in exactly one sentence.",
            name: "OneLiner");
        Console.WriteLine($"ChatClientAgent: {await agent.RunAsync("What is an AI agent?")}");

        // 3. The Responses API: the newer Foundry API that also powers the built-in tools (episode 21).
        AIAgent responsesAgent = foundry.GetResponsesClient().AsAIAgent(
            model: Foundry.ChatDeployment,
            instructions: "You answer in exactly one sentence.",
            name: "ResponsesAgent");
        Console.WriteLine($"Responses API : {await responsesAgent.RunAsync("What is Microsoft Foundry?")}");
    }
}
