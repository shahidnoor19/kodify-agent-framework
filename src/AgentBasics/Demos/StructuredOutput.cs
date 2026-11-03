using System.ComponentModel;
using Microsoft.Agents.AI;
using OpenAI.Chat;

namespace AgentBasics.Demos;

/// <summary>Episode 6: personas through instructions, and typed C# results through structured output.</summary>
public static class StructuredOutput
{
    public static async Task RunAsync()
    {
        var chatClient = Foundry.CreateClient().GetChatClient(Foundry.ChatDeployment);

        // Same model, two personas: only the instructions differ.
        AIAgent formal = chatClient.AsAIAgent(instructions: "You are a formal banking support officer. Two sentences maximum.");
        AIAgent casual = chatClient.AsAIAgent(instructions: "You are a relaxed gaming store helper who uses emojis. Two sentences maximum.");
        const string question = "My order is late. What should I do?";
        Console.WriteLine($"Formal: {await formal.RunAsync(question)}\n");
        Console.WriteLine($"Casual: {await casual.RunAsync(question)}\n");

        // Structured output: the model fills a C# record that matches a JSON schema.
        AIAgent triage = chatClient.AsAIAgent(
            name: "TriageAgent",
            instructions: "Classify customer support messages for Kodify Store.");

        AgentResponse<TicketClassification> result = await triage.RunAsync<TicketClassification>(
            "I was charged twice for order 1002 and I need my money back today!");

        TicketClassification ticket = result.Result;
        Console.WriteLine($"Category: {ticket.Category}, Priority: {ticket.Priority}, Sentiment: {ticket.Sentiment}");
        Console.WriteLine($"Summary : {ticket.Summary}");
    }
}

public enum TicketCategory { Billing, Technical, Orders, General }
public enum TicketPriority { Low, Medium, High, Urgent }

public sealed record TicketClassification(
    [property: Description("The team that should handle the ticket.")] TicketCategory Category,
    [property: Description("How urgent the ticket is.")] TicketPriority Priority,
    [property: Description("Customer mood: positive, neutral or negative.")] string Sentiment,
    [property: Description("One sentence summary of the problem.")] string Summary);
