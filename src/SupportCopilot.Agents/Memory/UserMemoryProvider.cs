using Microsoft.Agents.AI;
using SupportCopilot.Application.Security;

namespace SupportCopilot.Agents.Memory;

/// <summary>
/// Before each run, tells the agent who it is talking to. The caller comes from the run, not from HttpContext,
/// so it also works in background jobs and workflows. Video 18 grows this into long-term memory.
/// </summary>
public sealed class UserMemoryProvider(ICallerContextAccessor callers) : AIContextProvider
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var caller = callers.Current;
        if (caller == CallerContext.Anonymous) return ValueTask.FromResult(new AIContext());

        var who = caller.IsCustomer
            ? $"You are talking to the customer {caller.UserName}, email {caller.CustomerEmail}. Use this email for their orders and tickets."
            : $"You are talking to {caller.UserName}, a {caller.Role} on the support team.";
        return ValueTask.FromResult(new AIContext { Instructions = who });
    }
}
