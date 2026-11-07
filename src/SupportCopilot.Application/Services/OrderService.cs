using Microsoft.EntityFrameworkCore;
using SupportCopilot.Domain;

namespace SupportCopilot.Application.Services;

public sealed record OrderDto(int Id, string Product, decimal Amount, string Status, DateTime OrderedAt, string CustomerEmail);

/// <summary>
/// Orders for the agent tools, the dashboard and the workflows: one place for the rules.
/// Teaching version: video 12 adds the access rules to these same methods.
/// </summary>
public sealed class OrderService(IApplicationDbContext db)
{
    public async Task<OrderDto?> GetAsync(int orderId, CancellationToken ct = default)
    {
        var order = await db.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        return order is null ? null : ToDto(order);
    }

    public async Task<List<OrderDto>> ListForCustomerAsync(string customerEmail, CancellationToken ct = default)
    {
        var orders = await db.Orders.Include(o => o.Customer)
            .Where(o => o.Customer!.Email == customerEmail)
            .OrderByDescending(o => o.OrderedAt).ToListAsync(ct);
        return orders.Select(ToDto).ToList();
    }

    /// <summary>Dashboard: every order, newest first (video 12: a customer sees only their own).</summary>
    public async Task<List<OrderDto>> ListVisibleAsync(CancellationToken ct = default) =>
        (await db.Orders.Include(o => o.Customer).OrderByDescending(o => o.OrderedAt).ToListAsync(ct)).Select(ToDto).ToList();

    private static OrderDto ToDto(Order o) => new(o.Id, o.Product, o.Amount, o.Status, o.OrderedAt, o.Customer?.Email ?? "");
}
