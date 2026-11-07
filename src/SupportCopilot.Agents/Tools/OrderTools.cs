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

    /// <summary>Read tools run freely; video 13 adds the refund and cancel tools, behind human approval.</summary>
    public IList<AITool> ReadTools() => [AIFunctionFactory.Create(GetOrder), AIFunctionFactory.Create(ListCustomerOrders)];
}
