using Microsoft.EntityFrameworkCore;
using SupportCopilot.Domain;

namespace SupportCopilot.Application;

/// <summary>The data the application needs. Infrastructure implements it with EF Core + SQLite.</summary>
public interface IApplicationDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<Order> Orders { get; }
    DbSet<Ticket> Tickets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
