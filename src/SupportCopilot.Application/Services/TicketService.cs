using Microsoft.EntityFrameworkCore;
using SupportCopilot.Application.Security;
using SupportCopilot.Domain;

namespace SupportCopilot.Application.Services;

public sealed record CustomerDto(string Name, string Email, string Tier);
public sealed record TicketDto(int Id, string Title, string Category, string Priority, string Status, string CustomerEmail, DateTime CreatedAt);

/// <summary>Customers and support tickets, with the same access rules as <see cref="OrderService"/>.</summary>
public sealed class TicketService(IApplicationDbContext db, ICallerContextAccessor callers)
{
    private CallerContext Caller => callers.Current;

    public async Task<CustomerDto?> GetCustomerAsync(string email, CancellationToken ct = default)
    {
        if (!Caller.CanReadCustomer(email)) throw new AccessDeniedException("You can only see your own profile.");
        var c = await db.Customers.FirstOrDefaultAsync(x => x.Email == email, ct);
        return c is null ? null : new CustomerDto(c.Name, c.Email, c.Tier);
    }

    public async Task<TicketDto> CreateAsync(string title, string description, string category, string priority, string customerEmail, CancellationToken ct = default)
    {
        if (Caller == CallerContext.Anonymous) throw new AccessDeniedException("You cannot create tickets.");
        // A customer can only open tickets for themselves, whatever email the model passes.
        if (Caller.IsCustomer) customerEmail = Caller.CustomerEmail!;

        var ticket = new Ticket
        {
            Title = title,
            Description = description,
            Category = category,
            Priority = priority,
            CustomerEmail = customerEmail,
            CreatedBy = Caller.UserName,
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        return ToDto(ticket);
    }

    /// <summary>Open tickets: staff see all, a customer only their own.</summary>
    public async Task<List<TicketDto>> ListOpenAsync(CancellationToken ct = default)
    {
        var query = db.Tickets.Where(t => t.Status != "Closed");
        if (Caller.IsCustomer) query = query.Where(t => t.CustomerEmail == Caller.CustomerEmail);
        else if (!Caller.IsStaff) throw new AccessDeniedException("You cannot list tickets.");
        return (await query.OrderByDescending(t => t.CreatedAt).ToListAsync(ct)).Select(ToDto).ToList();
    }

    /// <summary>Dashboard list (latest 20), same visibility rules.</summary>
    public async Task<List<TicketDto>> ListRecentAsync(CancellationToken ct = default)
    {
        var query = db.Tickets.AsQueryable();
        if (Caller.IsCustomer) query = query.Where(t => t.CustomerEmail == Caller.CustomerEmail);
        else if (!Caller.IsStaff) return [];
        return (await query.OrderByDescending(t => t.CreatedAt).Take(20).ToListAsync(ct)).Select(ToDto).ToList();
    }

    private static TicketDto ToDto(Ticket t) => new(t.Id, t.Title, t.Category, t.Priority, t.Status, t.CustomerEmail, t.CreatedAt);
}
