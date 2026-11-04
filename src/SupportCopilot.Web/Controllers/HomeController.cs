using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportCopilot.Application;
using SupportCopilot.Domain;

namespace SupportCopilot.Web.Controllers;

public sealed record DashboardModel(List<Ticket> Tickets, List<Order> Orders);

/// <summary>Dashboard. Teaching version: it reads the DbContext interface directly; episode 11 moves this into services.</summary>
public sealed class HomeController(IApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(new DashboardModel(
            await db.Tickets.OrderByDescending(t => t.CreatedAt).ToListAsync(ct),
            await db.Orders.Include(o => o.Customer).OrderBy(o => o.Id).ToListAsync(ct)));

    public IActionResult Error() => Problem();
}
