using Microsoft.EntityFrameworkCore;
using SupportCopilot.Application.Security;
using SupportCopilot.Domain;

namespace SupportCopilot.Application.Services;

public sealed record UsageSummary(string AgentName, string Model, long InputTokens, long OutputTokens, int Runs);

/// <summary>Token usage per run, so a supervisor can answer: what does a ticket cost?</summary>
public sealed class UsageService(IApplicationDbContext db, ICallerContextAccessor callers)
{
    public async Task RecordAsync(string agentName, string model, long inputTokens, long outputTokens, CancellationToken ct = default)
    {
        db.UsageRecords.Add(new UsageRecord
        {
            UserName = callers.Current.UserName,
            AgentName = agentName,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<UsageSummary>> SummaryAsync(CancellationToken ct = default)
    {
        var rows = await db.UsageRecords.ToListAsync(ct);
        return rows.GroupBy(r => (r.AgentName, r.Model))
            .Select(g => new UsageSummary(g.Key.AgentName, g.Key.Model, g.Sum(r => r.InputTokens), g.Sum(r => r.OutputTokens), g.Count()))
            .OrderByDescending(s => s.InputTokens + s.OutputTokens)
            .ToList();
    }
}
