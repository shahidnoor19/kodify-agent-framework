namespace SupportCopilot.Domain;

public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Tier { get; set; } = "Standard";
    public List<Order> Orders { get; set; } = [];
}

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string Product { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = OrderStatus.Processing;
    public DateTime OrderedAt { get; set; }
}

public static class OrderStatus
{
    public const string Processing = "Processing";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Refunded = "Refunded";
    public const string Cancelled = "Cancelled";
}

public sealed class Ticket
{
    public int Id { get; set; }
    public string CustomerEmail { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "General";
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public string? Resolution { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A chat conversation. The agent session is stored as JSON so the chat survives a restart.</summary>
public sealed class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string UserName { get; set; } = "";
    public string AgentName { get; set; } = "";
    public string Title { get; set; } = "New chat";
    public string SessionJson { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A tool call waiting for a human. Stored so it survives restarts and so we know who may approve it.</summary>
public sealed class PendingApproval
{
    public string RequestId { get; set; } = "";
    public string ConversationId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string AgentName { get; set; } = "";
    public string ToolName { get; set; } = "";
    public string ArgumentsJson { get; set; } = "";
    public string RequestJson { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
