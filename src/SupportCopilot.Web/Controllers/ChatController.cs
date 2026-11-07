using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.AI;
using SupportCopilot.Agents;
using SupportCopilot.Agents.Conversations;
using SupportCopilot.Domain;
using SupportCopilot.Web.Infrastructure;

namespace SupportCopilot.Web.Controllers;

public sealed record ChatRequest(string? ConversationId, string Agent, string Message);
public sealed record ApprovalAnswer(string ConversationId, string RequestId, bool Approved, bool Always = false);

public sealed class ChatPageModel
{
    public required List<AgentInfo> Agents { get; init; }
    public required List<Conversation> Conversations { get; init; }
    public Conversation? Current { get; init; }
    public List<ChatLine> History { get; init; } = [];
}

/// <summary>Interactive chat: short turns stream inside the request (SSE).</summary>
[Authorize]
public sealed class ChatController(IServiceProvider services, ConversationStore store, ApprovalStore approvals, ILogger<ChatController> log) : Controller
{
    private string UserName => User.Identity!.Name!;

    private static readonly List<AgentInfo> ChatAgents = AgentRegistration.Catalog.Where(a => !a.RunsAsJob).ToList();

    public async Task<IActionResult> Index(string? id, CancellationToken ct)
    {
        var conversations = await store.ListAsync(UserName);
        var current = conversations.FirstOrDefault(c => c.Id == id);
        var history = current is null ? [] : await store.HistoryAsync(current, Agent(current.AgentName), ct);
        return View(new ChatPageModel { Agents = ChatAgents, Conversations = conversations, Current = current, History = history });
    }

    [HttpPost, EnableRateLimiting("agent")]
    public async Task Stream([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (ChatAgents.All(a => a.Key != request.Agent)) { Response.StatusCode = 404; return; }

        var agent = Agent(request.Agent);
        // One session per conversation: read back from SQLite, or a new one for a new chat.
        var (conversation, session) = await store.OpenAsync(request.ConversationId, UserName, request.Agent, agent, ct);
        await StreamAsync(agent, conversation, session, new ChatMessage(ChatRole.User, request.Message), request.Message, ct);
    }

    /// <summary>The conversation owner answers a tool approval; supervisor-only tools need a supervisor.</summary>
    [HttpPost, EnableRateLimiting("agent")]
    public async Task Approve([FromBody] ApprovalAnswer answer, CancellationToken ct)
    {
        var caller = HttpContext.RequestServices.GetRequiredService<Application.Security.ICallerContextAccessor>().Current;
        var (pending, request, error) = await approvals.TakeAsync(answer.RequestId, caller, ct);
        if (error is not null)
        {
            Sse.Start(Response);
            await Sse.SendAsync(Response, "error", new { message = error }, ct);
            await Sse.SendAsync(Response, "done", new { id = answer.ConversationId }, ct);
            return;
        }

        var agent = Agent(pending!.AgentName);
        var (conversation, session) = await store.OpenAsync(pending.ConversationId, UserName, pending.AgentName, agent, ct);
        // "Always approve" adds a standing rule, so the same tool stops asking in this conversation.
        AIContent response = answer.Approved && answer.Always
            ? request!.CreateAlwaysApproveToolResponse("Approved for this conversation.")
            : request!.CreateResponse(answer.Approved, answer.Approved ? null : "The reviewer rejected this action.");
        await StreamAsync(agent, conversation, session, new ChatMessage(ChatRole.User, [response]), "", ct);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await store.DeleteAsync(id, UserName);
        return RedirectToAction(nameof(Index));
    }

    private async Task StreamAsync(AIAgent agent, Conversation conversation, AgentSession session, ChatMessage message, string text, CancellationToken ct)
    {
        Sse.Start(Response);
        await Sse.SendAsync(Response, "conversation", new { id = conversation.Id }, ct);
        var approvalsToSave = new List<ToolApprovalRequestContent>();
        try
        {
            await foreach (var update in agent.RunStreamingAsync(message, session, cancellationToken: ct))
            {
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case ToolApprovalRequestContent approval when approval.ToolCall is FunctionCallContent call:
                            approvalsToSave.Add(approval);
                            await Sse.SendAsync(Response, "approval", new { requestId = approval.RequestId, tool = call.Name, arguments = call.Arguments }, ct);
                            break;
                        case FunctionCallContent call:
                            await Sse.SendAsync(Response, "tool", new { name = call.Name }, ct);
                            break;
                        case TextContent { Text.Length: > 0 } delta:
                            await Sse.SendAsync(Response, "delta", new { text = delta.Text }, ct);
                            break;
                    }
                }
            }
            // Save the conversation first, then the approvals that point to it.
            await store.SaveAsync(conversation, agent, session, text, ct);
            foreach (var approval in approvalsToSave)
                await approvals.SaveAsync(conversation.Id, UserName, conversation.AgentName, approval, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "Agent run failed");
            await Sse.SendAsync(Response, "error", new { message = "The agent failed. Please try again." }, ct);
        }
        await Sse.SendAsync(Response, "done", new { id = conversation.Id, title = conversation.Title }, ct);
    }

    private AIAgent Agent(string key) => services.GetRequiredKeyedService<AIAgent>(key);
}
