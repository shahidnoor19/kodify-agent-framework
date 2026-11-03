using System.ComponentModel;
using Microsoft.Agents.AI;
using OpenAI.Chat;

namespace AgentBasics.Tests;

/// <summary>
/// Episode 6: a first evaluation with a real model. A judge agent scores the support agent's answer
/// against a rubric (LLM-as-judge). Few, slow and not free: the unit tests do most of the work.
/// </summary>
public sealed class QualityTests(ITestOutputHelper output)
{
    public sealed record Verdict(
        [property: Description("1 to 5: is the answer polite and empathetic?")] int Tone,
        [property: Description("1 to 5: does it give the customer a clear next step?")] int NextStep,
        [property: Description("true if the answer invents facts that are neither in the question nor in the known facts.")] bool Hallucinated,
        [property: Description("One sentence explaining the scores.")] string Reason);

    [LiveFact]
    public async Task Support_answer_passes_the_quality_rubric()
    {
        ChatClient model = Foundry.CreateClient().GetChatClient(Foundry.ChatDeployment);
        const string facts = "Order 1002: Xbox Wireless Headset, $99, delivered 6 days ago. Faulty within 30 days of delivery: the customer chooses a refund or a replacement.";
        AIAgent support = model.AsAIAgent(instructions: $"You are a friendly support assistant for Kodify Store. Use only these facts: {facts}");
        AIAgent judge = model.AsAIAgent(instructions: "You are a strict QA reviewer of customer support answers.");

        const string question = "My headset from order 1002 stopped charging after 5 days. What can I do?";
        var answer = await support.RunAsync(question);

        // The judge must see the same facts the agent had, or it will call them hallucinations.
        var verdict = (await judge.RunAsync<Verdict>($"Known facts: {facts}\n\nQuestion: {question}\n\nAnswer: {answer.Text}")).Result;
        output.WriteLine($"Answer: {answer.Text}\nVerdict: {verdict}");

        Assert.True(verdict.Tone >= 3, verdict.Reason);
        Assert.True(verdict.NextStep >= 3, verdict.Reason);
        Assert.False(verdict.Hallucinated, verdict.Reason);
    }
}
