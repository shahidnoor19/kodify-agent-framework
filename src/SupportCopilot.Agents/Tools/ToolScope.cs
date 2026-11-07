using Microsoft.Extensions.DependencyInjection;
using SupportCopilot.Application.Security;

namespace SupportCopilot.Agents.Tools;

/// <summary>
/// Agents are singletons; services are scoped. Each tool call gets its own scope (and DbContext).
/// An access-denied error becomes a sentence the model can explain to the user.
/// </summary>
public sealed class ToolScope(IServiceScopeFactory scopes)
{
    public async Task<object> RunAsync<TService>(Func<TService, Task<object>> call) where TService : notnull
    {
        using var scope = scopes.CreateScope();
        try
        {
            return await call(scope.ServiceProvider.GetRequiredService<TService>());
        }
        catch (AccessDeniedException ex)
        {
            return $"Not allowed: {ex.Message} Tell the user politely; do not try another way around it.";
        }
    }
}
