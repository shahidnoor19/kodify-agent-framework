using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SupportCopilot.Application.Services;

namespace SupportCopilot.Web.Controllers;

public sealed record DashboardModel(List<TicketDto> Tickets, List<OrderDto> Orders, List<UsageSummary> Usage);

/// <summary>Dashboard. The same services as the agent tools, so the rules live in one place.</summary>
[Authorize]
public sealed class HomeController(TicketService tickets, OrderService orders, UsageService usage) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(new DashboardModel(
            await tickets.ListRecentAsync(ct),
            await orders.ListVisibleAsync(ct),
            User.IsInRole("Supervisor") ? await usage.SummaryAsync(ct) : []));

    public IActionResult Error() => Problem();
}
