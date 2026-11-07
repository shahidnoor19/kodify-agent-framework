using System.Text.Json;

namespace SupportCopilot.Web.Infrastructure;

/// <summary>Server-Sent Events. Each event is "event: name" + "data: json" + a blank line.</summary>
public static class Sse
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void Start(HttpResponse response)
    {
        response.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";
    }

    public static async Task SendAsync(HttpResponse response, string eventName, object data, CancellationToken ct = default)
    {
        await response.WriteAsync($"event: {eventName}\ndata: {JsonSerializer.Serialize(data, Json)}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }
}
