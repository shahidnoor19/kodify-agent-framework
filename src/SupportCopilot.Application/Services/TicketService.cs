using Microsoft.EntityFrameworkCore;
using SupportCopilot.Domain;

namespace SupportCopilot.Application.Services;

public sealed record CustomerDto(string Name, string Email, string Tier);
public sealed record TicketDto(int Id, string Title, string Category, string Priority, string Status, string CustomerEmail, DateTime CreatedAt);

/// <summary>Customers and support tickets. Teaching version: video 12 adds the same access rules as OrderService.</summary>
public sealed class TicketService(IApplicationDbContext db)
{
    public async Task<CustomerDto?> GetCustomerAsync(string email, CancellationToken ct = default)
    {
        var c = await db.Customers.FirstOrDefaultAsync(x => x.Email == email, ct);
        return c is null ? null : new CustomerDto(c.Name, c.Email, c.Tier);
    }

    public async Task<TicketDto> CreateAsync(string title, string description, string category, string priority, string customerEmail, CancellationToken ct = default)
    {
        var ticket = new Ticket
        {
            Title = title,
            Description = description,
            Category = category,
            Priority = priority,
            CustomerEmail = customerEmail,
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        return ToDto(ticket);
    }

    /// <summary>Open tickets, newest first (video 12: staff see all, a customer only their own).</summary>
    public async Task<List<TicketDto>> ListOpenAsync(CancellationToken ct = default) =>
        (await db.Tickets.Where(t => t.Status != "Closed").OrderByDescending(t => t.CreatedAt).ToListAsync(ct)).Select(ToDto).ToList();

    /// <summary>Dashboard list (latest 20).</summary>
    public async Task<List<TicketDto>> ListRecentAsync(CancellationToken ct = default) =>
        (await db.Tickets.OrderByDescending(t => t.CreatedAt).Take(20).ToListAsync(ct)).Select(ToDto).ToList();

    private static TicketDto ToDto(Ticket t) => new(t.Id, t.Title, t.Category, t.Priority, t.Status, t.CustomerEmail, t.CreatedAt);
}
