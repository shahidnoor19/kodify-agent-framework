using Microsoft.EntityFrameworkCore;
using SupportCopilot.Application;
using SupportCopilot.Domain;

namespace SupportCopilot.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<PendingApproval> PendingApprovals => Set<PendingApproval>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Order>().Property(o => o.Amount).HasConversion<double>();
        model.Entity<Customer>().HasIndex(c => c.Email).IsUnique();
    model.Entity<PendingApproval>().HasKey(p => p.RequestId);
    }
}
