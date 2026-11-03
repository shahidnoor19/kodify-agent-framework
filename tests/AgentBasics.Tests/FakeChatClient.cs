using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace AgentBasics.Tests;

/// <summary>
/// A fake model. It returns canned answers and records what the agent sent,
/// so agent code can be tested fast, offline and for free.
/// </summary>
public sealed class FakeChatClient(params string[] answers) : IChatClient
{
    private readonly Queue<string> _answers = new(answers);

    /// <summary>The messages of every call the agent made.</summary>
    public List<List<ChatMessage>> Calls { get; } = [];

    /// <summary>The options of every call (instructions travel here).</summary>
    public List<ChatOptions?> Options { get; } = [];

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Calls.Add(messages.ToList());
        Options.Add(options);
        var text = _answers.Count > 0 ? _answers.Dequeue() : "OK";
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var update in response.ToChatResponseUpdates()) yield return update;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
