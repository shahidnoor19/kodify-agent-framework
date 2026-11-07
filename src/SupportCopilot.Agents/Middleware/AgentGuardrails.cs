using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupportCopilot.Application.Services;
using SupportCopilot.Infrastructure.Options;

namespace SupportCopilot.Agents.Middleware;

/// <summary>
/// Middleware around every agent.
/// Run middleware: redacts PII before the model sees the message, then records token usage (video 15 adds content safety).
/// Function middleware: logs every tool call (names only, unless sensitive logging is on in Development).
/// Authorization is NOT here: it lives in the application services, where it also protects the UI and workflows.
/// </summary>
public sealed class AgentGuardrails(
    PiiRedactor redactor,
    IServiceScopeFactory scopes,
    IOptions<FoundryOptions> foundry,
    ILogger<AgentGuardrails> log)
{
    private bool LogSensitive => foundry.Value.LogSensitiveData;

    public AIAgent Apply(AIAgent agent, IServiceProvider services, string model) => agent.AsBuilder()
        .Use(
            (messages, session, options, inner, ct) => RunAsync(messages, session, options, inner, model, ct),
            (messages, session, options, inner, ct) => RunStreamingAsync(messages, session, options, inner, model, ct))
        .Use(InvokeToolAsync)
        .UseOpenTelemetry(sourceName: AgentTelemetry.SourceName, configure: t => t.EnableSensitiveData = LogSensitive)
        .Build(services);

    private async Task<AgentResponse> RunAsync(
        IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, AIAgent inner, string model, CancellationToken ct)
    {
        var response = await inner.RunAsync(Redact(messages), session, options, ct);
        await RecordUsageAsync(inner.Name, model, response.Usage, ct);
        return response;
    }

    private async IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(
        IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, AIAgent inner, string model,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var usage = new UsageDetails();
        await foreach (var update in inner.RunStreamingAsync(Redact(messages), session, options, ct))
        {
            foreach (var u in update.Contents.OfType<UsageContent>()) usage.Add(u.Details);
            yield return update;
        }
        await RecordUsageAsync(inner.Name, model, usage, ct);
    }

    /// <summary>Redacts user text. Images and approval answers pass through.</summary>
    private List<ChatMessage> Redact(IEnumerable<ChatMessage> messages)
    {
        var result = new List<ChatMessage>();
        foreach (var message in messages)
        {
            if (message.Role != ChatRole.User) { result.Add(message); continue; }

            var contents = message.Contents
                .Select(c => c is TextContent t ? new TextContent(redactor.Redact(t.Text)) : c)
                .ToList();
            result.Add(new ChatMessage(ChatRole.User, contents) { AuthorName = message.AuthorName });
        }
        return result;
    }

    private async ValueTask<object?> InvokeToolAsync(
        AIAgent agent, FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken ct)
    {
        if (LogSensitive)
            log.LogInformation("{Agent} calls {Tool}({Args})", agent.Name, context.Function.Name,
                string.Join(", ", context.Arguments.Select(a => $"{a.Key}={a.Value}")));
        else
            log.LogInformation("{Agent} calls {Tool}", agent.Name, context.Function.Name);

        return await next(context, ct);
    }

    private async Task RecordUsageAsync(string? agentName, string model, UsageDetails? usage, CancellationToken ct)
    {
        if (usage is null || (usage.InputTokenCount ?? 0) + (usage.OutputTokenCount ?? 0) == 0) return;
        using var scope = scopes.CreateScope();
        await scope.ServiceProvider.GetRequiredService<UsageService>()
            .RecordAsync(agentName ?? "agent", model, usage.InputTokenCount ?? 0, usage.OutputTokenCount ?? 0, ct);
    }
}

public static class AgentTelemetry
{
    /// <summary>The ActivitySource name for our agents. ServiceDefaults subscribes to it.</summary>
    public const string SourceName = "SupportCopilot";
}
