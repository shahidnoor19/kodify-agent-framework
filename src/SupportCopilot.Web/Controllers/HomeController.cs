using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SupportCopilot.Application.Services;

namespace SupportCopilot.Web.Controllers;

public sealed record DashboardModel(List<TicketDto> Tickets, List<OrderDto> Orders);

/// <summary>Dashboard. The same services as the agent tools, so the rules live in one place.</summary>
[Authorize]
public sealed class HomeController(TicketService tickets, OrderService orders) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(new DashboardModel(
            await tickets.ListRecentAsync(ct),
            await orders.ListVisibleAsync(ct)));

    public IActionResult Error() => Problem();
}
