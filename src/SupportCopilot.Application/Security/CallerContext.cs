namespace SupportCopilot.Application.Security;

public static class Roles
{
    /// <summary>Support staff: can read every customer, create and update tickets.</summary>
    public const string Agent = "Agent";
    /// <summary>Support lead: everything an agent can do, plus refunds and cancellations.</summary>
    public const string Supervisor = "Supervisor";
    /// <summary>A customer: only their own orders and tickets.</summary>
    public const string Customer = "Customer";
}

/// <summary>
/// Who is asking. It travels with the agent run (not through HttpContext), so the same rules apply
/// in a web request, a background job, a workflow or an A2A call.
/// </summary>
public sealed record CallerContext(string UserName, string Role, string? CustomerEmail = null)
{
    public static readonly CallerContext Anonymous = new("anonymous", "");

    public bool IsStaff => Role is Roles.Agent or Roles.Supervisor;
    public bool IsSupervisor => Role == Roles.Supervisor;
    public bool IsCustomer => Role == Roles.Customer;

    /// <summary>Staff can read any customer; a customer only themselves; anyone else nobody.</summary>
    public bool CanReadCustomer(string email) =>
        IsStaff || (IsCustomer && string.Equals(CustomerEmail, email, StringComparison.OrdinalIgnoreCase));
}

public sealed class AccessDeniedException(string message) : Exception(message);

/// <summary>The current caller, set by whoever starts the work (a request, a job, a workflow).</summary>
public interface ICallerContextAccessor
{
    CallerContext Current { get; }

    /// <summary>Sets the caller for the code inside the using block, including everything it awaits.</summary>
    IDisposable Begin(CallerContext caller);
}

/// <summary>AsyncLocal flows with async calls and Task.Run, so tools and background agents see the right caller.</summary>
public sealed class CallerContextAccessor : ICallerContextAccessor
{
    private static readonly AsyncLocal<CallerContext?> Value = new();

    public CallerContext Current => Value.Value ?? CallerContext.Anonymous;

    public IDisposable Begin(CallerContext caller)
    {
        var previous = Value.Value;
        Value.Value = caller;
        return new Restore(() => Value.Value = previous);
    }

    private sealed class Restore(Action undo) : IDisposable
    {
        public void Dispose() => undo();
    }
}
