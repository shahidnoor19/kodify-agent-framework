using SupportCopilot.Domain;

namespace SupportCopilot.Infrastructure.Data;

/// <summary>Creates the SQLite database with demo customers, orders and tickets.</summary>
public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        db.Database.EnsureCreated();
        if (db.Customers.Any()) return;

        var ayesha = new Customer { Name = "Ayesha Khan", Email = "ayesha@contoso.com", Tier = "Gold" };
        var omar = new Customer { Name = "Omar Farooq", Email = "omar@contoso.com" };
        var sara = new Customer { Name = "Sara Ahmed", Email = "sara@fabrikam.com", Tier = "Platinum" };
        db.Customers.AddRange(ayesha, omar, sara);

        var now = DateTime.UtcNow;
        db.Orders.AddRange(
            new Order { Id = 1001, Customer = ayesha, Product = "Surface Laptop 7", Amount = 1499m, Status = "Delivered", OrderedAt = now.AddDays(-20) },
            new Order { Id = 1002, Customer = ayesha, Product = "Xbox Wireless Headset", Amount = 99m, Status = "Delivered", OrderedAt = now.AddDays(-6) },
            new Order { Id = 1003, Customer = omar, Product = "4K Monitor 27\"", Amount = 379m, Status = "Shipped", OrderedAt = now.AddDays(-3) },
            new Order { Id = 1004, Customer = omar, Product = "USB-C Dock", Amount = 189m, Status = "Processing", OrderedAt = now.AddDays(-1) },
            new Order { Id = 1005, Customer = sara, Product = "Surface Pro 11", Amount = 1299m, Status = "Delivered", OrderedAt = now.AddDays(-40) });

        db.Tickets.AddRange(
            new Ticket { CustomerEmail = "omar@contoso.com", Title = "Monitor flickers", Description = "The new monitor flickers on HDMI.", Category = "Technical", Priority = "Medium", CreatedAt = now.AddDays(-1) },
            new Ticket { CustomerEmail = "sara@fabrikam.com", Title = "Invoice copy", Description = "Need a VAT invoice for order 1005.", Category = "Billing", Priority = "Low", CreatedAt = now.AddHours(-5) });

        db.SaveChanges();
    }
}
