using System.ComponentModel;
using Microsoft.Extensions.AI;
using SupportCopilot.Application.Services;

namespace SupportCopilot.Agents.Tools;

/// <summary>Customer and ticket tools: adapters over <see cref="TicketService"/>.</summary>
public sealed class TicketTools(ToolScope tools)
{
    [Description("Gets a customer's profile by email address.")]
    public Task<object> GetCustomer([Description("The customer's email address.")] string email) =>
        tools.RunAsync<TicketService>(async s => (object?)await s.GetCustomerAsync(email) ?? $"No customer with email {email}.");

    [Description("Creates a support ticket and returns its number.")]
    public Task<object> CreateTicket(
        [Description("Short title of the problem.")] string title,
        [Description("Full description of the problem.")] string description,
        [Description("Billing, Technical, Orders or General.")] string category,
        [Description("Low, Medium, High or Urgent.")] string priority,
        [Description("The customer's email address.")] string customerEmail) =>
        tools.RunAsync<TicketService>(async s =>
        {
            var ticket = await s.CreateAsync(title, description, category, priority, customerEmail);
            return $"Ticket #{ticket.Id} created ({ticket.Category}, {ticket.Priority}).";
        });

    [Description("Lists the open support tickets, newest first.")]
    public Task<object> ListOpenTickets() =>
        tools.RunAsync<TicketService>(async s => await s.ListOpenAsync());

    public IList<AITool> AllTools() =>
    [
        AIFunctionFactory.Create(GetCustomer),
        AIFunctionFactory.Create(CreateTicket),
        AIFunctionFactory.Create(ListOpenTickets),
    ];
}
