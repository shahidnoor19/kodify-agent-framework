using Microsoft.Extensions.DependencyInjection;

namespace SupportCopilot.Agents.Tools;

/// <summary>
/// Agents are singletons; services are scoped. Each tool call gets its own scope (and DbContext).
/// Video 12 adds: an access-denied error becomes a sentence the model can explain to the user.
/// </summary>
public sealed class ToolScope(IServiceScopeFactory scopes)
{
    public async Task<object> RunAsync<TService>(Func<TService, Task<object>> call) where TService : notnull
    {
        using var scope = scopes.CreateScope();
        return await call(scope.ServiceProvider.GetRequiredService<TService>());
    }
}
