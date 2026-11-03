using Microsoft.Agents.AI;
using OpenAI.Chat;

namespace AgentBasics.Demos;

/// <summary>Episode 4: stream the answer token by token, then keep a conversation going with a session.</summary>
public static class Streaming
{
    public static async Task RunAsync()
    {
        AIAgent agent = Foundry.CreateClient().GetChatClient(Foundry.ChatDeployment).AsAIAgent(
            name: "SupportAgent",
            instructions: "You are a friendly support assistant for Kodify Store, an online electronics shop.");

        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("Write three tips for keeping a laptop battery healthy."))
        {
            Console.Write(update.Text);
        }
        Console.WriteLine();

        // A session remembers the conversation, so follow-up questions work.
        AgentSession session = await agent.CreateSessionAsync();
        while (true)
        {
            Console.Write("\nYou: ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) break;

            Console.Write("Agent: ");
            await foreach (var update in agent.RunStreamingAsync(input, session))
            {
                Console.Write(update.Text);
            }
            Console.WriteLine();
        }
    }
}
