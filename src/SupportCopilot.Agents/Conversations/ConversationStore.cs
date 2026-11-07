using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using SupportCopilot.Application;
using SupportCopilot.Domain;

namespace SupportCopilot.Agents.Conversations;

public sealed record ChatLine(string Role, string Text);

/// <summary>One AgentSession per conversation, serialized to JSON and saved, so a chat continues after a restart.</summary>
public sealed class ConversationStore(IApplicationDbContext db)
{
    public Task<List<Conversation>> ListAsync(string userName) =>
        db.Conversations.Where(c => c.UserName == userName).OrderByDescending(c => c.UpdatedAt).ToListAsync();

    public async Task<(Conversation Conversation, AgentSession Session)> OpenAsync(
        string? id, string userName, string agentKey, AIAgent agent, CancellationToken ct)
    {
        var conversation = id is null ? null
            : await db.Conversations.FirstOrDefaultAsync(c => c.Id == id && c.UserName == userName, ct);

        if (conversation is null || conversation.AgentName != agentKey || conversation.SessionJson.Length == 0)
        {
            conversation ??= new Conversation { UserName = userName, AgentName = agentKey };
            return (conversation, await agent.CreateSessionAsync(ct));
        }

        return (conversation, await DeserializeAsync(agent, conversation.SessionJson, ct));
    }

    public async Task SaveAsync(Conversation conversation, AIAgent agent, AgentSession session, string firstMessage, CancellationToken ct)
    {
        var json = await agent.SerializeSessionAsync(session, cancellationToken: ct);
        conversation.SessionJson = json.GetRawText();
        conversation.UpdatedAt = DateTime.UtcNow;
        if (conversation.Title == "New chat" && firstMessage.Length > 0)
            conversation.Title = firstMessage.Length > 40 ? firstMessage[..40] + "..." : firstMessage;

        if (!await db.Conversations.AnyAsync(c => c.Id == conversation.Id, ct)) db.Conversations.Add(conversation);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Reads the messages back out of the saved session to redraw the chat.</summary>
    public async Task<List<ChatLine>> HistoryAsync(Conversation conversation, AIAgent agent, CancellationToken ct)
    {
        if (conversation.SessionJson.Length == 0) return [];
        var session = await DeserializeAsync(agent, conversation.SessionJson, ct);
        if (!session.TryGetInMemoryChatHistory(out var messages)) return [];

        return messages
            .Where(m => (m.Role == ChatRole.User || m.Role == ChatRole.Assistant) && !string.IsNullOrWhiteSpace(m.Text))
            .Select(m => new ChatLine(m.Role.Value, m.Text))
            .ToList();
    }

    public Task DeleteAsync(string id, string userName) =>
        db.Conversations.Where(c => c.Id == id && c.UserName == userName).ExecuteDeleteAsync();

    public static async Task<AgentSession> DeserializeAsync(AIAgent agent, string sessionJson, CancellationToken ct)
    {
        using var json = JsonDocument.Parse(sessionJson);
        return await agent.DeserializeSessionAsync(json.RootElement.Clone(), cancellationToken: ct);
    }
}
