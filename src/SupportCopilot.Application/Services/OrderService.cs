using Microsoft.EntityFrameworkCore;
using SupportCopilot.Application.Security;
using SupportCopilot.Domain;

namespace SupportCopilot.Application.Services;

public sealed record OrderDto(int Id, string Product, decimal Amount, string Status, DateTime OrderedAt, string CustomerEmail);

/// <summary>
/// Orders for the agent tools, the dashboard and the workflows: one place for the rules.
/// The access rules live here, so the model can never see or change more than the signed-in caller could.
/// </summary>
public sealed class OrderService(IApplicationDbContext db, ICallerContextAccessor callers)
{
    private CallerContext Caller => callers.Current;

    public async Task<OrderDto?> GetAsync(int orderId, CancellationToken ct = default)
    {
        var order = await db.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) return null;

        EnsureCanRead(order.Customer!.Email);
        return ToDto(order);
    }

    public async Task<List<OrderDto>> ListForCustomerAsync(string customerEmail, CancellationToken ct = default)
    {
        EnsureCanRead(customerEmail);
        var orders = await db.Orders.Include(o => o.Customer)
            .Where(o => o.Customer!.Email == customerEmail)
            .OrderByDescending(o => o.OrderedAt).ToListAsync(ct);
        return orders.Select(ToDto).ToList();
    }

    /// <summary>Dashboard: staff see every order, a customer only their own.</summary>
    public async Task<List<OrderDto>> ListVisibleAsync(CancellationToken ct = default)
    {
        var query = db.Orders.Include(o => o.Customer).AsQueryable();
        if (Caller.IsCustomer) query = query.Where(o => o.Customer!.Email == Caller.CustomerEmail);
        else if (!Caller.IsStaff) return [];
        return (await query.OrderByDescending(o => o.OrderedAt).ToListAsync(ct)).Select(ToDto).ToList();
    }

    private void EnsureCanRead(string customerEmail)
    {
        if (!Caller.CanReadCustomer(customerEmail))
            throw new AccessDeniedException("You can only see your own orders.");
    }

    private static OrderDto ToDto(Order o) => new(o.Id, o.Product, o.Amount, o.Status, o.OrderedAt, o.Customer?.Email ?? "");
}
