using System.Text.RegularExpressions;

namespace SupportCopilot.Agents.Middleware;

/// <summary>Removes card numbers, CNIC numbers and phone numbers before a message reaches the model.</summary>
public sealed partial class PiiRedactor
{
    [GeneratedRegex(@"\b\d(?:[ -]?\d){12,15}\b")]
    private static partial Regex CardNumber();

    [GeneratedRegex(@"\b\d{5}-\d{7}-\d\b")]
    private static partial Regex Cnic();

    [GeneratedRegex(@"(?<!\w)\+?\d{2,3}[ -]?\d{3}[ -]?\d{7}\b")]
    private static partial Regex Phone();

    public string Redact(string text)
    {
        // CNIC first: its 13 digits would otherwise look like a card number.
        text = Cnic().Replace(text, "[CNIC REDACTED]");
        text = CardNumber().Replace(text, "[CARD REDACTED]");
        return Phone().Replace(text, "[PHONE REDACTED]");
    }
}
