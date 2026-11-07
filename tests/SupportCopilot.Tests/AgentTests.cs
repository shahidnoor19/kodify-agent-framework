using SupportCopilot.Agents.Middleware;

namespace SupportCopilot.Tests;

public sealed class AgentTests
{
    [Theory]
    [InlineData("My card is 4111 1111 1111 1111", "My card is [CARD REDACTED]")]
    [InlineData("CNIC 42101-1234567-1 please", "CNIC [CNIC REDACTED] please")]
    [InlineData("Call me on +92 300 1234567", "Call me on [PHONE REDACTED]")]
    [InlineData("Order 1002 for ayesha@contoso.com", "Order 1002 for ayesha@contoso.com")]
    public void Redactor_removes_personal_numbers_but_keeps_orders_and_emails(string input, string expected)
    {
        Assert.Equal(expected, new PiiRedactor().Redact(input));
    }
}
