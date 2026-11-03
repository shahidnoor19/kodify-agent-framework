using Microsoft.Agents.AI;
using OpenAI.Chat;

namespace AgentBasics.Demos;

/// <summary>Episode 3: the smallest possible agent, a model plus instructions.</summary>
public static class FirstAgent
{
    public static async Task RunAsync()
    {
        ChatClient chatClient = Foundry.CreateClient().GetChatClient(Foundry.ChatDeployment);

        AIAgent agent = chatClient.AsAIAgent(
            name: "SupportAgent",
            instructions: "You are a friendly support assistant for Kodify Store, an online electronics shop. Keep answers short.");

        AgentResponse response = await agent.RunAsync("Hi! What can you help me with?");
        Console.WriteLine(response.Text);
    }
}
