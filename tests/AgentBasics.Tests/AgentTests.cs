using AgentBasics.Demos;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentBasics.Tests;

/// <summary>Episode 6: unit tests for agent code with a fake model.</summary>
public sealed class AgentTests
{
    [Fact]
    public async Task Agent_returns_the_model_answer()
    {
        var model = new FakeChatClient("Hello from the fake model");
        AIAgent agent = model.AsAIAgent(instructions: "Be nice.");

        var response = await agent.RunAsync("Hi");

        Assert.Equal("Hello from the fake model", response.Text);
    }

    [Fact]
    public async Task Instructions_travel_with_every_call()
    {
        var model = new FakeChatClient("One", "Two");
        AIAgent agent = model.AsAIAgent(instructions: "Be nice.");

        await agent.RunAsync("Hi");
        await agent.RunAsync("Hi again");

        Assert.All(model.Options, o => Assert.Equal("Be nice.", o?.Instructions));
    }

    [Fact]
    public async Task Session_sends_earlier_turns_to_the_model()
    {
        var model = new FakeChatClient("First answer", "Second answer");
        AIAgent agent = model.AsAIAgent(instructions: "Be nice.");
        var session = await agent.CreateSessionAsync();

        await agent.RunAsync("My name is Ayesha.", session);
        await agent.RunAsync("What is my name?", session);

        var secondCall = model.Calls[1].Select(m => m.Text).ToList();
        Assert.Contains("My name is Ayesha.", secondCall);
        Assert.Contains("First answer", secondCall);
    }

    [Fact]
    public async Task Without_a_session_the_agent_forgets()
    {
        var model = new FakeChatClient("First answer", "Second answer");
        AIAgent agent = model.AsAIAgent(instructions: "Be nice.");

        await agent.RunAsync("My name is Ayesha.");
        await agent.RunAsync("What is my name?");

        Assert.DoesNotContain("My name is Ayesha.", model.Calls[1].Select(m => m.Text));
    }

    [Fact]
    public async Task Session_survives_serialization()
    {
        var model = new FakeChatClient("Noted.");
        AIAgent agent = model.AsAIAgent(instructions: "Be nice.");
        var session = await agent.CreateSessionAsync();
        await agent.RunAsync("Remember order 1002.", session);

        var json = await agent.SerializeSessionAsync(session);
        var restored = await agent.DeserializeSessionAsync(json);

        Assert.True(restored.TryGetInMemoryChatHistory(out var messages));
        Assert.Equal(2, messages.Count);
    }

    [Fact]
    public async Task Streaming_returns_the_same_text_in_pieces()
    {
        var model = new FakeChatClient("Hello there");
        AIAgent agent = model.AsAIAgent(instructions: "Be nice.");

        var text = "";
        await foreach (var update in agent.RunStreamingAsync("Hi")) text += update.Text;

        Assert.Equal("Hello there", text);
    }

    [Fact]
    public async Task Structured_output_becomes_a_typed_record()
    {
        var model = new FakeChatClient("""{"category":"Billing","priority":"Urgent","sentiment":"negative","summary":"Charged twice for order 1002."}""");
        AIAgent triage = model.AsAIAgent(instructions: "Classify support messages.");

        var result = await triage.RunAsync<TicketClassification>("I was charged twice for order 1002!");

        Assert.Equal(TicketCategory.Billing, result.Result.Category);
        Assert.Equal(TicketPriority.Urgent, result.Result.Priority);
    }
}
