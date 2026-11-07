using System.ComponentModel;
using Microsoft.Extensions.AI;
using SupportCopilot.Application.Services;

namespace SupportCopilot.Agents.Tools;

/// <summary>
/// Order tools. They are thin adapters: the rules live in <see cref="OrderService"/>, not in the tools.
/// </summary>
public sealed class OrderTools(ToolScope tools)
{
    [Description("Gets one order by its order number.")]
    public Task<object> GetOrder([Description("The order number, for example 1002.")] int orderId) =>
        tools.RunAsync<OrderService>(async s => (object?)await s.GetAsync(orderId) ?? $"Order {orderId} was not found.");

    [Description("Lists all orders for a customer email address.")]
    public Task<object> ListCustomerOrders([Description("The customer's email address.")] string customerEmail) =>
        tools.RunAsync<OrderService>(async s => await s.ListForCustomerAsync(customerEmail));

    [Description("Refunds a delivered order. Needs human approval.")]
    public Task<object> RefundOrder(
        [Description("The order number to refund.")] int orderId,
        [Description("Why the customer wants a refund.")] string reason) =>
        tools.RunAsync<OrderService>(async s => await s.RefundAsync(orderId, reason));

    [Description("Cancels an order that has not shipped yet. Needs human approval.")]
    public Task<object> CancelOrder([Description("The order number to cancel.")] int orderId) =>
        tools.RunAsync<OrderService>(async s => await s.CancelAsync(orderId));

    /// <summary>Tools that move money; approving them needs the Supervisor role.</summary>
    public static readonly IReadOnlySet<string> SupervisorTools = new HashSet<string> { nameof(RefundOrder), nameof(CancelOrder) };

    public IList<AITool> ReadTools() => [AIFunctionFactory.Create(GetOrder), AIFunctionFactory.Create(ListCustomerOrders)];

    /// <summary>Read tools run freely; money-moving tools wait for a human to approve.</summary>
    public IList<AITool> AllTools() =>
    [
        .. ReadTools(),
        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(RefundOrder)),
        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(CancelOrder)),
    ];
}
