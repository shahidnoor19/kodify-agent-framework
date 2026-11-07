using System.Text.Json;
using Microsoft.Extensions.AI;
using SupportCopilot.Agents.Tools;
using SupportCopilot.Application;
using SupportCopilot.Application.Security;
using SupportCopilot.Domain;

namespace SupportCopilot.Agents.Conversations;

/// <summary>
/// Tool approvals waiting for a human, stored in the database: they survive restarts,
/// and we always know who asked and who is allowed to answer.
/// </summary>
public sealed class ApprovalStore(IApplicationDbContext db)
{
    public async Task SaveAsync(string conversationId, string userName, string agentName, ToolApprovalRequestContent request, CancellationToken ct)
    {
        var call = (FunctionCallContent)request.ToolCall;
        db.PendingApprovals.Add(new PendingApproval
        {
            RequestId = request.RequestId,
            ConversationId = conversationId,
            UserName = userName,
            AgentName = agentName,
            ToolName = call.Name,
            ArgumentsJson = JsonSerializer.Serialize(call.Arguments),
            RequestJson = JsonSerializer.Serialize<AIContent>(request, AIJsonUtilities.DefaultOptions),
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Loads a pending approval the caller is allowed to answer, or explains why not.</summary>
    public async Task<(PendingApproval? Pending, ToolApprovalRequestContent? Request, string? Error)> TakeAsync(
        string requestId, CallerContext caller, CancellationToken ct)
    {
        var pending = await db.PendingApprovals.FindAsync([requestId], ct);
        if (pending is null) return (null, null, "This approval request was not found or was already answered.");
        if (pending.UserName != caller.UserName) return (null, null, "You can only answer approvals in your own conversations.");
        if (OrderTools.SupervisorTools.Contains(pending.ToolName) && !caller.IsSupervisor)
            return (null, null, $"Only a supervisor can approve {pending.ToolName}. Please use the refund workflow so a supervisor can review it.");

        var request = (ToolApprovalRequestContent)JsonSerializer.Deserialize<AIContent>(pending.RequestJson, AIJsonUtilities.DefaultOptions)!;
        db.PendingApprovals.Remove(pending);
        await db.SaveChangesAsync(ct);
        return (pending, request, null);
    }
}
