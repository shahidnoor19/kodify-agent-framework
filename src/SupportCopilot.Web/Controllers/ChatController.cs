using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.AI;
using SupportCopilot.Agents;
using SupportCopilot.Web.Infrastructure;

namespace SupportCopilot.Web.Controllers;

public sealed record ChatRequest(string Agent, string Message);

public sealed class ChatPageModel
{
    public required List<AgentInfo> Agents { get; init; }
}

/// <summary>Interactive chat: short turns stream inside the request (SSE).</summary>
public sealed class ChatController(IServiceProvider services, ILogger<ChatController> log) : Controller
{
    private static readonly List<AgentInfo> ChatAgents = AgentRegistration.Catalog.Where(a => !a.RunsAsJob).ToList();

    public IActionResult Index() => View(new ChatPageModel { Agents = ChatAgents });

    [HttpPost, EnableRateLimiting("agent")]
    public async Task Stream([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (ChatAgents.All(a => a.Key != request.Agent)) { Response.StatusCode = 404; return; }

        var agent = Agent(request.Agent);
        // A new session for every message for now: video 10 keeps one per conversation, in SQLite.
        var session = await agent.CreateSessionAsync(ct);
        await StreamAsync(agent, session, new ChatMessage(ChatRole.User, request.Message), ct);
    }

    private async Task StreamAsync(AIAgent agent, AgentSession session, ChatMessage message, CancellationToken ct)
    {
        Sse.Start(Response);
        try
        {
            await foreach (var update in agent.RunStreamingAsync(message, session, cancellationToken: ct))
            {
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case TextContent { Text.Length: > 0 } delta:
                            await Sse.SendAsync(Response, "delta", new { text = delta.Text }, ct);
                            break;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "Agent run failed");
            await Sse.SendAsync(Response, "error", new { message = "The agent failed. Please try again." }, ct);
        }
        await Sse.SendAsync(Response, "done", new { }, ct);
    }

    private AIAgent Agent(string key) => services.GetRequiredKeyedService<AIAgent>(key);
}
